using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using EntityMatch;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Adapters;
using Newtonsoft.Json.Linq;

namespace TestMatcher
{
	class Program
	{
		static void ReadPhrases(string path, IMatcher matcher)
		{
			var timer = Stopwatch.StartNew();
			int count = 0;
			using (var stream = new StreamReader(path))
			{
				var splitter = new Regex("(?:^|,)(?:(?:\"((?:[^\"]|\"\")*)\")|([^,\"]*))", RegexOptions.Compiled);
				Func<string, string[]> split = (input) => (from Match match in splitter.Matches(input) select match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).ToArray();
				var columns = split(stream.ReadLine()!);
				while (!stream.EndOfStream)
				{
					var line = stream.ReadLine();
					var values = split(line!);
					matcher.AddEntities("DVD", values[0]);
					++count;
				}
			}
			matcher.Compute();
			matcher.Interpretations("dkjkjh", 1, 0.0).ToList();
			Console.WriteLine($"Reading {count} phrases from {path} took {timer.Elapsed.TotalSeconds}s");
		}

		static async Task TestLoop(IMatcher matcher)
		{
			int spansPerPosition = 1;
			double threshold = 0.25;
			string luisAPIKey = string.Empty;
			string luisAppId = string.Empty;
			string? input;
			Console.WriteLine($"-threshold {threshold} will set the threshold for dropping matches.");
			Console.WriteLine($"-spans {spansPerPosition} will control how many matches per range.");
			Console.WriteLine($"-luisapikey {luisAPIKey} will control the LUIS apiKey to use.");
			Console.WriteLine($"-luisappid {luisAppId} will control the LUIS appId to use.");
			Console.Write("\nInput: ");

			while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
			{
				if (input.StartsWith("-threshold"))
				{
					if (double.TryParse(input.Substring("-threshold".Length), out threshold))
					{
						Console.WriteLine($"Set threshold to {threshold}");
					}
					else
					{
						Console.WriteLine("Could not parse threshold");
					}
				}
				else if (input.StartsWith("-spans"))
				{
					if (int.TryParse(input.Substring("-spans".Length), out spansPerPosition))
					{
						Console.WriteLine($"Set spans to {spansPerPosition}");
					}
					else
					{
						Console.WriteLine("Could not parse spans");
					}
				}
				else if (input.StartsWith("-luisapikey"))
				{
					luisAPIKey = input.Substring("-luisapikey".Length);
				}
				else if (input.StartsWith("-luisappid"))
				{
					luisAppId = input.Substring("-luisappid".Length);
				}
				else
				{
					// read parameters that may be different for each input: 
					// - isDebug: start input with character "0"
					// - luisAppId (to be able to access different models): start input with character 
					// that corresponds to the case statement below
					// e.g. 05<yourinput> -> will run as isDebug with appid 5

					// isDebug parameter
					int isDebugIndicator;
					bool isDebug = false;
					if (int.TryParse(input[0].ToString(), out isDebugIndicator)
						&& isDebugIndicator == 0)
					{
						isDebug = true;
						input = input.Substring(1, input.Length - 1);
					}

					// luisAppId parameter (can also be controlled globally)
					int version;
					if (int.TryParse(input[0].ToString(), out version))
					{
						input = input.Substring(1, input.Length - 1);
					}

					var timer = Stopwatch.StartNew();
					var interpretations = matcher.Interpretations(input, spansPerPosition, threshold).ToList();
					if (isDebug)
					{
						Console.WriteLine($"Interpretation took {timer.Elapsed.TotalSeconds}s");
					}
					var externalEntities = new List<Microsoft.Bot.Builder.AI.LuisV3.ExternalEntity>();
					foreach (var interpretation in interpretations)
					{
						if (isDebug || String.IsNullOrEmpty(luisAPIKey))
						{
							Console.WriteLine($"{interpretation}");
						}
						var tokens = interpretation.GetTokens();
						foreach (Span span in interpretation.Spans)
						{
							// TODO(sonjak): add parameter/investigate how this relates to the threshold above
							//if (span.Score >= 0.99)
							if (span.Score >= 0.2)
							{
								var spanTokens = new List<string>();
								var posStart = span.Start;
								for (var pos = 0; pos < span.Length; ++pos)
								{
									spanTokens.Add(tokens[posStart + pos]);
								}
								var stringToFind = string.Join(" ", spanTokens);
								var indexStart = input.ToLower().IndexOf(stringToFind);
								var length = stringToFind.Length;

								if (indexStart != -1)
								{
									// TODO(sonjak): add configuration
									var entityType = "MovieC";
									externalEntities.Add(new Microsoft.Bot.Builder.AI.LuisV3.ExternalEntity(entityType, indexStart, length));
								}
								else
								{
									Console.WriteLine($"{stringToFind} not found");
								}
							}
						}
					}

					if (!String.IsNullOrEmpty(luisAPIKey))
					{
						if (isDebug)
						{
							Console.WriteLine("Results with external entities");
						}

						var entitiesWithEE = await Run(input, externalEntities, luisAPIKey, luisAppId, isDebug);

						if (isDebug)
						{
							Console.WriteLine("Results without external entities");
						}

						string entitiesWithoutEE = await Run(input, new List<Microsoft.Bot.Builder.AI.LuisV3.ExternalEntity>(), luisAPIKey, luisAppId, isDebug);

						Console.WriteLine($"{entitiesWithEE}/{entitiesWithoutEE}");
					}
				}
				Console.Write("\nInput: ");
			}
		}

		public static ITurnContext GetContext(string utterance)
		{
			var testAdapter = new TestAdapter();
			var activity = new Microsoft.Bot.Schema.Activity
			{
				Type = ActivityTypes.Message,
				Text = utterance,
				Conversation = new ConversationAccount(),
				Recipient = new ChannelAccount(),
				From = new ChannelAccount(),
			};
			return new TurnContext(testAdapter, activity);
		}

		static async Task<string> Run(string input, List<Microsoft.Bot.Builder.AI.LuisV3.ExternalEntity> externalEntities,
			string luisAPIKey,
			string luisAppId,
			bool isDebug)
		{
			if (isDebug)
			{
				Console.WriteLine($"AppId: {luisAppId}");
			}
			var luisAPIHostName = "westus.api.cognitive.microsoft.com";
			var luisApplication = new LuisApplication(
				luisAppId, //configuration["LuisAppId"],
				luisAPIKey, //configuration["LuisAPIKey"],
				"https://" + luisAPIHostName //"https://" + configuration["LuisAPIHostName"]);
				);

			// Set the recognizer options depending on which endpoint version you want to use.
			// More details can be found in https://docs.microsoft.com/en-gb/azure/cognitive-services/luis/luis-migration-api-v3
			var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication)
			{
				PredictionOptions = new Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions
				{
					IncludeInstanceData = true,
					ExternalEntities = externalEntities,
					PreferExternalEntities = true,
					// additional options
					//IncludeAllIntents = true,
					//IncludeAPIResults = true,
				}
			};

			var recognizer = new LuisRecognizer(recognizerOptions);
			var context = GetContext(input);
			var result = await recognizer.RecognizeAsync(context, null);
			var entities = new List<string>();
			foreach (KeyValuePair<string, JToken?> keyValuePair in result.Entities)
			{
				if ("$instance" == keyValuePair.Key)
				{
					if (isDebug)
					{
						Console.WriteLine(keyValuePair.Value);
					}
					var instances = ((JContainer)keyValuePair.Value!).Children<JProperty>();
					foreach (var instance in instances)
					{
						// TODO(sonjak): add configuration
						if (instance.Name == "Movie" || instance.Name == "Yes")
						{
							var value = (JObject)((JArray)instance.Value).First();
							foreach (JProperty property in value.Children())
							{
								if (property.Name == "text")
								{
									if (isDebug)
									{
										Console.WriteLine($"Found: {property.Value}");
									}
									entities.Add(property.Value.ToString());
								}
							}
							break;
						}
					}
				}
			}
			return string.Join(", ", entities);
		}


		private static IContainer? Container { get; set; }

		static void Main(string[] args)
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<SimpleTokenizer>().As<ITokenizer>().SingleInstance();
			builder.RegisterType<EntitiesDictionary>().As<IEntities>().SingleInstance();
			builder.Register((c) => new SynonymAlternatives(
				// new BaseAlternatives()))
				new SpellingAlternatives(new BaseAlternatives())))
				.As<IAlternatives>()
				.As<SynonymAlternatives>()
				.SingleInstance();
			builder.RegisterType<Recognizer>().As<IEntityRecognizer>().SingleInstance();
			builder.RegisterType<Matcher>().As<IMatcher>(); ;
			Container = builder.Build();
			using (var scope = Container.BeginLifetimeScope())
			{
				var matcher = scope.Resolve<IMatcher>();
				var synonyms = scope.Resolve<SynonymAlternatives>();
				// TODO(sonjak): add back synonyms
				//synonyms.AddAlternatives("mouse", new Alternative("mouse", 1.0), new Alternative("mice", 0.9));
				ReadPhrases(@"DVD.txt", matcher);

				TestLoop(matcher).Wait();
			}
		}
	}
}
