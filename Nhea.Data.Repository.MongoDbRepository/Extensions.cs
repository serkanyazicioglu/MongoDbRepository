using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nhea.Data.Repository.MongoDbRepository
{
    public static class Extensions
    {
        public static async Task<List<T>> ToMongoListAsync<T>(this IQueryable<T> mongoQueryOnly)
        {
            if (mongoQueryOnly is EnumerableQuery)
            {
                return mongoQueryOnly.ToList();
            }

            return await mongoQueryOnly.ToListAsync();
        }
    }
}