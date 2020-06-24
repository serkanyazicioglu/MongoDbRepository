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

            return await mongoQueryOnly.ToMongoQueryable().ToListAsync();
        }

        private static IMongoQueryable<T> ToMongoQueryable<T>(this IQueryable<T> mongoQueryOnly)
        {
            return (IMongoQueryable<T>)mongoQueryOnly;
        }

        //public static IMongoQueryable<T> ToMongoQueryable<T>(this List<T> mongoQueryOnly)
        //{
        //    return (IMongoQueryable<T>)mongoQueryOnly;
        //}
    }
}