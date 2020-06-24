using Nhea.Data.Repository.MongoDbRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestProject.Repositories
{
    public abstract class BaseMongoDbRepository<T> : Nhea.Data.Repository.MongoDbRepository.BaseMongoDbRepository<T> where T : MongoDocument, new()
    {
        public BaseMongoDbRepository(bool isReadOnly = false)
            : base(isReadOnly)
        {
        }

        public override string ConnectionString => "mongodb://localhost:27017/admin";

        protected override string DefaultDatabaseName => "NheaTestDb";
    }
}
