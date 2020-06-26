using System;
using System.Collections.Generic;
using System.Linq;

namespace EntityMatch
{
    /// <summary>
    /// Stores a mapping between token string and entities
    /// This is done by tracking two data structures:
    /// _tokenToEntities tracks the mapping from token string to a list of EntityPositions.
    /// Supports entity strings occurring multiple times in the input string
    /// An EntityPosition stores the index of the Entity in _entities, and its start token position in the input
    /// _entities stores the list of Entity objects, which can be retrieved using the id in EntityPosition
    /// </summary>
    public class EntitiesDictionary : IEntities
    {
        private Dictionary<string, List<EntityPosition>> _tokenToEntities = new Dictionary<string, List<EntityPosition>>();
        private List<Entity> _entities = new List<Entity>();

        public IEnumerable<Entity> Entities
        {
            get
            {
                return _entities;
            }
        }

        public Entity this[int id]
        {
            get { return _entities[id]; }
        }

        /// <summary>
        /// Adds an array of entities to the EntitiesDictionary
        /// Their position can overlap with the entities already in the dictionary
        /// Supports entity strings occurring multiple times in the input string
        /// </summary>
        /// <param name="entities"></param>
        public void AddEntities(params Entity[] entities)
        {
            // id is the start id we use to store this new list of entities
            int id = _entities.Count();
            foreach (var entity in entities)
            {
                _entities.Add(entity);
                for (int i = 0; i < entity.Tokens.Length; ++i)
                {
                    var position = new EntityPosition(id, i);
                    var tokenString = entity.Tokens[i].TokenString;
                    List<EntityPosition> matches;
                    if (_tokenToEntities.TryGetValue(tokenString, out matches))
                    {
                        matches.Add(position);
                    }
                    else
                    {
                        _tokenToEntities[tokenString] = new List<EntityPosition> { position };
                    }
                }
                ++id;
            }
        }

        /// <summary>
        /// TODO(sonjak): document
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public double TokenWeight(string token)
        {
            var weight = 0.0d;
            List<EntityPosition> positions;
            if (_tokenToEntities.TryGetValue(token, out positions))
            {
                double count = positions.Count();
                weight = Math.Log((_entities.Count() + 0.5 - count) / (count + 0.5));
            }
            return Math.Max(weight, 0.00001d);
        }
        
        /// <summary>
        /// Compute entity weights
        /// Entities with more tokens get higher weights (it's the sum of the weights)
        /// </summary>
        public void Compute()
        {
            foreach (var entity in Entities)
            {
                var weight = 0.0d;
                foreach (var token in entity.Tokens)
                {
                    weight += TokenWeight(token.TokenString);
                }
                entity.TotalTokenWeight = weight;
            }
        }

        /// <summary>
        /// Get the list of entities for a token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public IEnumerable<EntityPosition> TokenEntities(string token)
        {
            List<EntityPosition> entities;
            if (!_tokenToEntities.TryGetValue(token, out entities))
            {
                entities = new List<EntityPosition>();
            }
            return entities;
        }
    }
}
