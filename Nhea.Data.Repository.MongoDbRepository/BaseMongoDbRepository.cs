using MongoDB.Bson;
using MongoDB.Driver;
using Nhea.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nhea.Data.Repository.MongoDbRepository
{
    public abstract class BaseMongoDbRepository<T> : Nhea.Data.Repository.BaseRepository<T>, IDisposable where T : MongoDocument, new()
    {
        public abstract string ConnectionString { get; }

        protected virtual string CollectionName
        {
            get
            {
                return typeof(T).Name;
            }
        }

        private MongoClient currentClient = null;
        private MongoClient CurrentClient
        {
            get
            {
                if (currentClient == null)
                {
                    currentClient = new MongoClient(this.ConnectionString);
                }

                return currentClient;
            }
        }

        public virtual string DatabaseName => MongoUrl.Create(this.ConnectionString).DatabaseName;

        private IMongoDatabase currentDatabase = null;
        private IMongoDatabase CurrentDatabase
        {
            get
            {
                if (currentDatabase == null)
                {
                    currentDatabase = CurrentClient.GetDatabase(DatabaseName);
                }

                return currentDatabase;
            }
        }

        private IMongoCollection<T> currentCollection = null;
        public IMongoCollection<T> CurrentCollection
        {
            get
            {
                if (currentCollection == null)
                {
                    currentCollection = CurrentDatabase.GetCollection<T>(this.CollectionName);
                }

                return currentCollection;
            }
        }

        private Dictionary<ObjectId, T> Items = new Dictionary<ObjectId, T>();

        private Dictionary<ObjectId, string> DirtyCheckItems = new Dictionary<ObjectId, string>();

        private object lockObject = new object();

        public override T CreateNew()
        {
            var entity = new T();
            entity._id = ObjectId.GenerateNewId();

            lock (lockObject)
            {
                Items.Add(entity._id, entity);
            }

            return entity;
        }

        private void AddCore(T entity, bool isNew)
        {
            lock (lockObject)
            {
                if (entity != null)
                {
                    if (Items.ContainsKey(entity._id))
                    {
                        Items.Remove(entity._id);
                    }

                    Items.Add(entity._id, entity);

                    if (!isNew)
                    {
                        if (!DirtyCheckItems.ContainsKey(entity._id))
                        {
                            DirtyCheckItems.Add(entity._id, entity.ToJson());
                        }
                    }
                }
            }
        }

        public void Remove(T entity)
        {
            lock (lockObject)
            {
                if (entity != null)
                {
                    Items.Remove(entity._id);
                }
            }
        }

        private static ConcurrentDictionary<string, object> LockObjects = new ConcurrentDictionary<string, object>();

        public override T GetById(object id)
        {
            string idParsed = id.ToString();

            var entity = GetByIdCore(idParsed);

            if (entity != null)
            {
                this.AddCore(entity, false);
            }

            return entity;
        }

        private T GetByIdCore(string id)
        {
            return GetByIdCore(ObjectId.Parse(id));
        }

        private T GetByIdCore(ObjectId id)
        {
            return CurrentCollection.Find(query => query._id == id).FirstOrDefault();
        }

        protected override T GetSingleCore(System.Linq.Expressions.Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            if (getDefaultFilter)
            {
                filter = filter.And(this.DefaultFilter);
            }

            var entity = CurrentCollection.Find(filter).SingleOrDefault();

            if (entity != null)
            {
                this.AddCore(entity, false);
            }

            return entity;
        }

        protected override IQueryable<T> GetAllCore(System.Linq.Expressions.Expression<Func<T, bool>> filter, bool getDefaultFilter, bool getDefaultSorter, string sortColumn, SortDirection? sortDirection, bool allowPaging, int pageSize, int pageIndex, ref int totalCount)
        {
            if (getDefaultFilter)
            {
                filter = filter.And(this.DefaultFilter);
            }

            if (filter == null)
            {
                filter = query => true;
            }

            IQueryable<T> returnList = CurrentCollection.Find(filter).ToList().AsQueryable();

            if (!String.IsNullOrEmpty(sortColumn))
            {
                returnList = returnList.Sort(sortColumn, sortDirection);
            }
            else if (getDefaultSorter && DefaultSorter != null)
            {
                if (DefaultSortType == SortDirection.Ascending)
                {
                    returnList = returnList.OrderBy(DefaultSorter);
                }
                else
                {
                    returnList = returnList.OrderByDescending(DefaultSorter);
                }
            }

            if (allowPaging && pageSize > 0)
            {
                if (totalCount == 0)
                {
                    totalCount = returnList.Count();
                }

                int skipCount = pageSize * pageIndex;

                returnList = returnList.Skip<T>(skipCount).Take<T>(pageSize);
            }

            foreach (var entity in returnList)
            {
                this.AddCore(entity, false);
            }

            return returnList;
        }

        public override void Add(T entity)
        {
            this.AddCore(entity, true);
        }

        public override void Add(List<T> entities)
        {
            foreach (var entity in entities)
            {
                this.AddCore(entity, true);
            }
        }

        public override void Delete(System.Linq.Expressions.Expression<Func<T, bool>> filter)
        {
            CurrentCollection.DeleteMany(filter);
        }

        public override void Delete(T entity)
        {
            CurrentCollection.DeleteOne(query => query._id == entity._id);
        }

        public override void Dispose()
        {
            this.DirtyCheckItems = null;
            this.Items = null;
            this.currentClient = null;
            this.currentDatabase = null;
        }

        public override bool IsNew(T entity)
        {
            return !entity.ModifyDate.HasValue;
        }

        public override void Refresh(T entity)
        {
            throw new NotImplementedException();
        }

        public bool HasChanges(T entity)
        {
            if (DirtyCheckItems.ContainsKey(entity._id))
            {
                var newItem = entity.ToJson();

                return newItem != DirtyCheckItems[entity._id];
            }

            return true;
        }

        public override void Save()
        {
            var savingList = Items.Values.ToList();

            for (int i = 0; i < savingList.Count(); i++)
            {
                var item = savingList[i];

                try
                {
                    if (HasChanges(item))
                    {
                        item.ModifyDate = DateTime.Now;

                        var replaceOneResult = CurrentCollection.ReplaceOne(query => query._id == item._id, item, new UpdateOptions { IsUpsert = true });

                        if (DirtyCheckItems.ContainsKey(item._id))
                        {
                            DirtyCheckItems.Remove(item._id);
                        }

                        DirtyCheckItems.Add(item._id, item.ToJson());
                    }
                }
                catch (Exception ex)
                {
                    ex.Data.Add("Id", item._id.ToString());
                    Logger.Log(ex);
                    throw;
                }
            }
        }

        protected override bool AnyCore(System.Linq.Expressions.Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            if (getDefaultFilter && this.DefaultFilter != null)
            {
                filter = filter.And(this.DefaultFilter);
            }

            if (filter == null)
            {
                filter = query => true;
            }

            return CurrentCollection.CountDocuments(filter) > 0;
        }

        protected override int CountCore(System.Linq.Expressions.Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            if (getDefaultFilter && this.DefaultFilter != null)
            {
                filter = filter.And(this.DefaultFilter);
            }

            if (filter == null)
            {
                filter = query => true;
            }

            return Convert.ToInt32(CurrentCollection.CountDocuments(filter));
        }

        public delegate void SubscriptionTriggeredEventHandler(object sender, T entity);
        public event SubscriptionTriggeredEventHandler SubscriptionTriggered;

        private static BaseMongoDbRepository<T> CurrentSubscribingRepository = null;

        private static object subscriberLockObject = new object();

        public static BaseMongoDbRepository<T> GetSubscribingRepository()
        {
            if (CurrentSubscribingRepository == null)
            {
                lock (subscriberLockObject)
                {
                    var currentDocumentType = typeof(T);
                    var currentAssembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
                        .Single(query => query.FullName == currentDocumentType.Assembly.FullName);

                    foreach (Type exportedType in currentAssembly.GetExportedTypes().Where(query => query.BaseType != null && query.BaseType.UnderlyingSystemType != null && query.BaseType.UnderlyingSystemType.IsGenericType))
                    {
                        if (exportedType.BaseType.UnderlyingSystemType.GetGenericArguments().FirstOrDefault() == currentDocumentType)
                        {
                            CurrentSubscribingRepository = Activator.CreateInstance(exportedType) as BaseMongoDbRepository<T>;

                            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<T>>().Match("{ operationType: /^[^d]/  }");
                            ChangeStreamOptions options = new ChangeStreamOptions() { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
                            var changeStream = CurrentSubscribingRepository.CurrentCollection.Watch(pipeline, options).ToEnumerable().GetEnumerator();

                            var task = Task.Run(() =>
                            {
                                while (true)
                                {
                                    try
                                    {
                                        changeStream.MoveNext();
                                        ChangeStreamDocument<T> next = changeStream.Current;
                                        var currentData = next.FullDocument;

                                        if (CurrentSubscribingRepository.SubscriptionTriggered == null)
                                        {
                                            continue;
                                        }

                                        var receivers = CurrentSubscribingRepository.SubscriptionTriggered.GetInvocationList();
                                        foreach (SubscriptionTriggeredEventHandler receiver in receivers)
                                        {
                                            receiver.BeginInvoke(CurrentSubscribingRepository, currentData, null, null);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                }
                            });

                            break;
                        }
                    }
                }
            }

            return CurrentSubscribingRepository;
        }
    }
}
