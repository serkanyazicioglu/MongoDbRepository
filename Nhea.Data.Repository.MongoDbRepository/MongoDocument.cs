using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Nhea.Data.Repository.MongoDbRepository
{
    public abstract class MongoDocument
    {
        [BsonId]
        public virtual ObjectId _id { get; set; }

        public DateTime? ModifyDate { get; set; }
    }
}
