using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Nhea.Data.Repository.MongoDbRepository;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestProject.Repositories;

namespace TestProject
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestAllSync()
        {
            ObjectId newMemberId = ObjectId.Empty;

            //New Entity
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.CreateNew();
                member.Title = "Test Member";
                member.UserName = "username";
                member.Password = "password";
                member.Email = "test@test.com";
                memberRepository.Save();

                newMemberId = member._id;
            }

            //Update Multiple Entity
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var members = memberRepository.GetAll(query => query._id >= new ObjectId(DateTime.Today, 0, 0, 0)).ToList();

                Assert.IsTrue(members.Any());

                foreach (var member in members)
                {
                    member.Title += " Lastname";
                }

                memberRepository.Save();
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.GetById(newMemberId);

                Assert.IsTrue(member.Title == "Test Member Lastname");
            }

            //Switch to readonly
            using (MemberRepository memberRepository = new MemberRepository(isReadOnly: true))
            {
                var member = memberRepository.GetById(newMemberId);
                member.Title = "You shall not change";
                memberRepository.Save();
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.GetById(newMemberId);

                Assert.IsTrue(member.Title == "Test Member Lastname");
            }

            //Update Single Entity By Id
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.GetById(newMemberId);

                Assert.IsTrue(member != null);

                if (member != null)
                {
                    member.Title = "Selected Member";
                    memberRepository.Save();
                }
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.GetById(newMemberId);

                Assert.IsTrue(member.Title == "Selected Member");
            }

            //Update Single Entity By Query
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.GetSingle(query => query.Title == "Selected Member");

                Assert.IsTrue(member != null);

                if (member != null)
                {
                    member.Title = "Selected Member 2";
                    memberRepository.Save();
                }
            }

            //Delete Entity
            using (MemberRepository memberRepository = new MemberRepository())
            {
                memberRepository.Delete(query => query.Title == "Selected Member 2");
                memberRepository.Save();
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var doesCurrentMemberExist = memberRepository.Any(query => query._id == newMemberId);

                Assert.IsFalse(doesCurrentMemberExist);
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var allMembers = memberRepository.GetAll().ToList();

                var membersCount = memberRepository.Count();

                Assert.IsTrue(allMembers.Count() == membersCount);
            }

            //IsNew
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.CreateNew();
                Console.WriteLine("Is my entity new? Answer: " + memberRepository.IsNew(member));
            }

            //Get Multiple & Migrate to Another Db
            using (MemberRepository memberRepository = new MemberRepository())
            {
                memberRepository.GetAll();

                memberRepository.DatabaseName = "AnotherDbName";
                memberRepository.CollectionName = "AnotherCollectionName";

                memberRepository.Save();
            }

            ////Use this only when you have replication enabled
            //var subscribingRepository = MemberRepository.GetSubscribingRepository();
            //subscribingRepository.SubscriptionTriggered += SubscribingRepository_SubscriptionTriggered;
        }

        [TestMethod]
        public async Task TestAllAsync()
        {
            ObjectId newMemberId = ObjectId.Empty;

            //New Entity
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.CreateNew();
                member.Title = "Test Member";
                member.UserName = "username";
                member.Password = "password";
                member.Email = "test@test.com";
                await memberRepository.SaveAsync();

                newMemberId = member._id;
            }

            //Update Multiple Entity
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var members = await memberRepository.GetAll(query => query._id >= new ObjectId(DateTime.Today, 0, 0, 0)).ToMongoListAsync();

                Assert.IsTrue(members.Any());

                foreach (var member in members)
                {
                    member.Title += " Lastname";
                }

                await memberRepository.SaveAsync();
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = await memberRepository.GetByIdAsync(newMemberId);

                Assert.IsTrue(member.Title == "Test Member Lastname");
            }

            //Update Single Entity By Id
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = await memberRepository.GetByIdAsync(newMemberId);

                Assert.IsTrue(member != null);

                if (member != null)
                {
                    member.Title = "Selected Member";
                    await memberRepository.SaveAsync();
                }
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = await memberRepository.GetByIdAsync(newMemberId);

                Assert.IsTrue(member.Title == "Selected Member");
            }

            //Switch to readonly
            using (MemberRepository memberRepository = new MemberRepository(isReadOnly: true))
            {
                var member = await memberRepository.GetByIdAsync(newMemberId);
                member.Title = "You shall not change";
                await memberRepository.SaveAsync();
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = await memberRepository.GetByIdAsync(newMemberId);

                Assert.IsTrue(member.Title == "Selected Member");
            }

            //Update Single Entity By Query
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = await memberRepository.GetSingleAsync(query => query.Title == "Selected Member");

                Assert.IsTrue(member != null);

                if (member != null)
                {
                    member.Title = "Selected Member 2";
                    await memberRepository.SaveAsync();
                }
            }

            //Delete Entity
            using (MemberRepository memberRepository = new MemberRepository())
            {
                memberRepository.Delete(query => query.Title == "Selected Member 2");
                await memberRepository.SaveAsync();
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var doesCurrentMemberExist = await memberRepository.AnyAsync(query => query._id == newMemberId);

                Assert.IsFalse(doesCurrentMemberExist);
            }

            using (MemberRepository memberRepository = new MemberRepository())
            {
                var allMembers = await memberRepository.GetAll().ToMongoListAsync();

                var membersCount = await memberRepository.CountAsync();

                Assert.IsTrue(allMembers.Count() == membersCount);
            }

            //IsNew
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.CreateNew();
                Console.WriteLine("Is my entity new? Answer: " + memberRepository.IsNew(member));
            }

            //Get Multiple
            using (MemberRepository memberRepository = new MemberRepository())
            {
                memberRepository.GetAll();

                memberRepository.DatabaseName = "AnotherDbName";
                memberRepository.CollectionName = "AnotherCollectionName";

                await memberRepository.SaveAsync();
            }

            ////Use this only when you have replication enabled
            //var subscribingRepository = MemberRepository.GetSubscribingRepository();
            //subscribingRepository.SubscriptionTriggered += SubscribingRepository_SubscriptionTriggered;
        }

        private static void SubscribingRepository_SubscriptionTriggered(object sender, Member entity)
        {
            Console.WriteLine("Subscription triggered: " + entity.Title);
        }
    }
}
