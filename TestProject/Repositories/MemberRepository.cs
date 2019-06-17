using Nhea.Data;
using Nhea.Data.Repository.MongoDbRepository;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace TestProject.Repositories
{
    public partial class Member : MongoDocument
    {
        public string Title { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public int Status { get; set; }

        public string Email { get; set; }
    }


    public class MemberRepository : BaseMongoDbRepository<Member>
    {
        public override Member CreateNew()
        {
            var entity = base.CreateNew();
            entity.Status = (int)StatusType.Available;
            return entity;
        }

        //Override to map to a different database. Set DatabaseName property on the fly if you want to change it.
        //public override string DefaultDatabaseName => base.DefaultDatabaseName;

        //Override to change target collection name. By default uses given type name for collection name. Set CollectionName property on the fly if you want to change it
        //public override string DefaultCollectionName => base.DefaultCollectionName;
    }
}
