using Nhea.Data.Repository.MongoDbRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleApp.Repositories
{
    public abstract class BaseMongoDbRepository<T> : Nhea.Data.Repository.MongoDbRepository.BaseMongoDbRepository<T> where T : MongoDocument, new()
    {
        public override string ConnectionString => "mongodb://localhost:27017/admin";

        public override string DefaultDatabaseName => "NheaTestDb";
    }
}
