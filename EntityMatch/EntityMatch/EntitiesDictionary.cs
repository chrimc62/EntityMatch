using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatch
{
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

        public void AddEntities(params Entity[] entities)
        {
            int id = _entities.Count();
            foreach (var entity in entities)
            {
                _entities.Add(entity);
                for (int i = 0; i < entity.Tokens.Length; ++i)
                {
                    var position = new EntityPosition(id, i);
                    var token = entity.Tokens[i];
                    List<EntityPosition> matches;
                    if (_tokenToEntities.TryGetValue(token, out matches))
                    {
                        matches.Add(position);
                    }
                    else
                    {
                        _tokenToEntities[token] = new List<EntityPosition> { position };
                    }
                }
                ++id;
            }
        }

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
