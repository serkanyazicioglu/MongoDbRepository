﻿using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nhea.Data.Repository.MongoDbRepository
{
    public abstract class BaseMongoDbRepository<T> : Nhea.Data.Repository.BaseRepository<T> where T : MongoDocument, new()
    {
        public BaseMongoDbRepository(bool isReadOnly = false)
            : base(isReadOnly)
        {
        }

        public abstract string ConnectionString { get; }

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

        protected virtual string DefaultDatabaseName => MongoUrl.Create(this.ConnectionString).DatabaseName;

        private string databaseName = null;
        public string DatabaseName
        {
            get
            {
                if (string.IsNullOrEmpty(databaseName))
                {
                    currentDatabase = null;
                    currentCollection = null;
                    this.DirtyCheckItems.Clear();
                    databaseName = DefaultDatabaseName;
                }

                return databaseName;
            }
            set
            {
                if (databaseName != value)
                {
                    currentDatabase = null;
                    currentCollection = null;
                    this.DirtyCheckItems.Clear();
                    databaseName = value;
                }
            }
        }

        protected virtual string DefaultCollectionName => typeof(T).Name;

        private string collectionName = null;
        public string CollectionName
        {
            get
            {
                if (string.IsNullOrEmpty(collectionName))
                {
                    currentCollection = null;
                    this.DirtyCheckItems.Clear();
                    collectionName = DefaultCollectionName;
                }

                return collectionName;
            }
            set
            {
                if (collectionName != value)
                {
                    currentCollection = null;
                    this.DirtyCheckItems.Clear();
                    collectionName = value;
                }
            }
        }

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

        public Dictionary<ObjectId, T> Items = new();

        private Dictionary<ObjectId, string> DirtyCheckItems = new();

        private object lockObject = new();

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
            if (this.IsReadOnly)
            {
                return;
            }

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
            if (this.IsReadOnly)
            {
                return;
            }

            lock (lockObject)
            {
                if (entity != null)
                {
                    Items.Remove(entity._id);
                }
            }
        }

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

        public override async Task<T> GetByIdAsync(object id)
        {
            string idParsed = id.ToString();

            var entity = await GetByIdCoreAsync(idParsed);

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

        private async Task<T> GetByIdCoreAsync(string id)
        {
            return await GetByIdCoreAsync(ObjectId.Parse(id));
        }

        private async Task<T> GetByIdCoreAsync(ObjectId id)
        {
            return await CurrentCollection.Find(query => query._id == id).FirstOrDefaultAsync();
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

        protected override async Task<T> GetSingleCoreAsync(System.Linq.Expressions.Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            if (getDefaultFilter)
            {
                filter = filter.And(this.DefaultFilter);
            }

            var entity = await CurrentCollection.Find(filter).SingleOrDefaultAsync();

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

            IQueryable<T> returnList = CurrentCollection.AsQueryable();

            if (filter != null)
            {
                returnList = returnList.Where(filter);
            }

            if (!string.IsNullOrEmpty(sortColumn))
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

            if (!this.IsReadOnly)
            {
                returnList = returnList.ToList().AsQueryable();

                foreach (var entity in returnList)
                {
                    this.AddCore(entity, false);
                }
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
            if (this.IsReadOnly)
            {
                return;
            }

            CurrentCollection.DeleteMany(filter);
        }

        public override void Delete(T entity)
        {
            if (this.IsReadOnly)
            {
                return;
            }

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
            if (this.IsReadOnly)
            {
                return;
            }

            var savingList = Items.Values.ToList();

            for (int i = 0; i < savingList.Count(); i++)
            {
                var item = savingList[i];

                if (HasChanges(item))
                {
                    bool isNew = this.IsNew(item);

                    item.ModifyDate = DateTime.Now;

                    if (isNew)
                    {
                        CurrentCollection.InsertOne(item);
                    }
                    else
                    {
                        var replaceOneResult = CurrentCollection.ReplaceOne(query => query._id == item._id, item, new ReplaceOptions { IsUpsert = true });
                    }

                    if (DirtyCheckItems.ContainsKey(item._id))
                    {
                        DirtyCheckItems.Remove(item._id);
                    }

                    DirtyCheckItems.Add(item._id, item.ToJson());
                }
            }
        }

        public override async Task SaveAsync()
        {
            if (this.IsReadOnly)
            {
                return;
            }

            var savingList = Items.Values.ToList();

            for (int i = 0; i < savingList.Count(); i++)
            {
                var item = savingList[i];

                if (HasChanges(item))
                {
                    bool isNew = this.IsNew(item);

                    item.ModifyDate = DateTime.Now;

                    if (isNew)
                    {
                        await CurrentCollection.InsertOneAsync(item);
                    }
                    else
                    {
                        var replaceOneResult = await CurrentCollection.ReplaceOneAsync(query => query._id == item._id, item, new ReplaceOptions { IsUpsert = true });
                    }

                    if (DirtyCheckItems.ContainsKey(item._id))
                    {
                        DirtyCheckItems.Remove(item._id);
                    }

                    DirtyCheckItems.Add(item._id, item.ToJson());
                }
            }
        }

        private Expression<Func<T, bool>> SetFilter(Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            if (getDefaultFilter && DefaultFilter != null)
            {
                filter = filter.And(DefaultFilter);
            }

            if (filter == null)
            {
                filter = query => true;
            }

            return filter;
        }

        protected override bool AnyCore(System.Linq.Expressions.Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            return CurrentCollection.CountDocuments(SetFilter(filter, getDefaultFilter)) > 0;
        }

        protected override async Task<bool> AnyCoreAsync(Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            return await CurrentCollection.CountDocumentsAsync(SetFilter(filter, getDefaultFilter)) > 0;
        }

        protected override int CountCore(System.Linq.Expressions.Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            return Convert.ToInt32(CurrentCollection.CountDocuments(SetFilter(filter, getDefaultFilter)));
        }

        protected override async Task<int> CountCoreAsync(Expression<Func<T, bool>> filter, bool getDefaultFilter)
        {
            return Convert.ToInt32(await CurrentCollection.CountDocumentsAsync(SetFilter(filter, getDefaultFilter)));
        }

        public delegate void SubscriptionTriggeredEventHandler(object sender, T entity);
        public event SubscriptionTriggeredEventHandler SubscriptionTriggered;

        private static BaseMongoDbRepository<T> CurrentSubscribingRepository = null;

        private static readonly Lock subscriberLockObject = new();

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
                            ChangeStreamOptions options = new() { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
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
                                    catch
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
