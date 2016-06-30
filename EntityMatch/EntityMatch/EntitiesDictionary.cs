using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
    public class EntitiesDictionary : IEntities
    {
        private Dictionary<string, List<int>> _tokenToEntities = new Dictionary<string, List<int>>();
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

        public void AddEntities(params Entity[] entities)
        {
            int id = _entities.Count();
            foreach (var entity in entities)
            {
                _entities.Add(entity);
                foreach (var token in entity.Tokens.Distinct())
                {
                    List<int> matches;
                    if (_tokenToEntities.TryGetValue(token, out matches))
                    {
                        matches.Add(id);
                    }
                    else
                    {
                        _tokenToEntities[token] = new List<int> { id };
                    }
                }
                ++id;
            }
        }

        public IEnumerable<int> TokenEntities(string token)
        {
            List<int> entities;
            if (!_tokenToEntities.TryGetValue(token, out entities))
            {
                entities = new List<int>();
            }
            return entities;
        }
    }
}
