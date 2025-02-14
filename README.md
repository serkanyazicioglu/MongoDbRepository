[![.NET](https://github.com/serkanyazicioglu/MongoDbRepository/actions/workflows/dotnet-pipeline.yml/badge.svg)](https://github.com/serkanyazicioglu/MongoDbRepository/actions/workflows/dotnet-pipeline.yml)
[![NuGet](https://img.shields.io/nuget/v/Nhea.Data.Repository.MongoDbRepository.svg)](https://www.nuget.org/packages/Nhea.Data.Repository.MongoDbRepository/)

# Nhea MongoDb Repository

MongoDb base repository classes.


## Getting Started

Nhea MongoDb Repository is on NuGet. You may install Nhea MongoDb Repository via NuGet Package manager.

https://www.nuget.org/packages/Nhea.Data.Repository.MongoDbRepository/

```
Install-Package Nhea.Data.Repository.MongoDbRepository
```

### Prerequisites

Project is built with .NET 9.0.

### Configuration

First of all creating a base repository class is a good idea to set basic properties like connection string.

```
public abstract class BaseMongoDbRepository<T> : Nhea.Data.Repository.MongoDbRepository.BaseMongoDbRepository<T> where T : MongoDocument, new()
{
    public override string ConnectionString => "mongodb://localhost:27017/admin";

    protected override string DefaultDatabaseName => "TestDb";
}
```
You may remove the abstract modifier if you want to use generic repositories or you may create individual repository classes for each of your objects if you need to set specific properties.
```
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
```
Then in your code just initalize a new instance of your class and call appropriate methods for your needs.

```
ObjectId newMemberId = ObjectId.GenerateNewId();

using (MemberRepository memberRepository = new MemberRepository())
{
    var member = memberRepository.CreateNew();
    member._id = newMemberId;
    member.Title = "Test Member";
    member.UserName = "username";
    member.Password = "password";
    member.Email = "test@test.com";
    memberRepository.Save();
}

using (MemberRepository memberRepository = new MemberRepository())
{
    var members = memberRepository.GetAll(query => query._id >= new ObjectId(DateTime.Today, 0, 0, 0)).ToList();

    foreach (var member in members)
    {
        member.Title += " Lastname";
    }

    memberRepository.Save();
}

using (MemberRepository memberRepository = new MemberRepository())
{
    var member = memberRepository.GetById(newMemberId);

    if (member != null)
    {
        member.Title = "Selected Member";
        memberRepository.Save();
    }
}

using (MemberRepository memberRepository = new MemberRepository())
{
    var member = memberRepository.GetSingle(query => query.Title == "Selected Member");

    if (member != null)
    {
        member.Title = "Selected Member 2";
        memberRepository.Save();
    }
}

using (MemberRepository memberRepository = new MemberRepository())
{
    memberRepository.Delete(query => query.Title == "Selected Member 2");
    memberRepository.Save();
}

using (MemberRepository memberRepository = new MemberRepository())
{
    var member = memberRepository.CreateNew();
    bool isNew = memberRepository.IsNew(member);
}

using (MemberRepository memberRepository = new MemberRepository())
{
    memberRepository.GetAll();

    memberRepository.DatabaseName = "AnotherDbName";
    memberRepository.CollectionName = "AnotherCollectionName";

    memberRepository.Save();
}
```
### Async List

You can use all methods with usual async sytnax but Mongo has it's own collection type for async collection fetching. You can use ToMongoListAsync extension method instead.
```
using (MemberRepository memberRepository = new MemberRepository())
{
    var members = await memberRepository.GetAll(query => query._id >= new ObjectId(DateTime.Today, 0, 0, 0)).ToMongoListAsync();
}
```
### ReadOnly Mode

You can set readonly mode for repositories. This may increase the performance when you don't need the data to be updated. Simply set a constructor on your repository to pass the isReadOnly parameter to base repo.
```
public MemberRepository(bool isReadOnly = false)
    : base(isReadOnly)
{
}
```
```
//Switch to readonly
using (MemberRepository memberRepository = new MemberRepository(isReadOnly: true))
{
    var member = await memberRepository.GetByIdAsync(newMemberId);
    member.Title = "You shall not change";
    await memberRepository.SaveAsync();
}
```


### Dynamic attributes

Repositories rely on strict document types because of object mapping. You may add custom Bson objects by specifing BsonDocument properties.
```
[BsonExtraElements]
public BsonDocument AttributeValue { get; set; }
```
```
member.AttributeValue.Add("CustomJsonData", jsonData.ToString());
```

### Subscription
In order to use this feature your MongoDb server must have replication enabled. MongoDb servers send messages between these servers to duplicate datas so we actually use this channels for listening document changes.

First you have to get an instance of subscribing repository. This is a static instance so you will get the same object everytime you call this method. Later bind an event to 'SubscriptionTriggered'.
```
var subscribingRepository = MemberRepository.GetSubscribingRepository();
subscribingRepository.SubscriptionTriggered += SubscribingRepository_SubscriptionTriggered;
```
Then all you have to do is just listening to this callback.
```
private static void SubscribingRepository_SubscriptionTriggered(object sender, Member entity)
{
    Console.WriteLine("Subscription triggered: " + entity.Title);
}
```
