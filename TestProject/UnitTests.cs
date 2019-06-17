using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using System;
using TestProject.Repositories;

namespace TestProject
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestAll()
        {
            ObjectId newMemberId = ObjectId.GenerateNewId();

            //New Entity
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

            //Update Multiple Entity
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var members = memberRepository.GetAll(query => query._id >= new ObjectId(DateTime.Today, 0, 0, 0)).ToList();

                foreach (var member in members)
                {
                    member.Title += " Lastname";
                }

                memberRepository.Save();
            }

            //Update Single Entity By Id
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.GetById(newMemberId);

                if (member != null)
                {
                    member.Title = "Selected Member";
                    memberRepository.Save();
                }
            }

            //Update Single Entity By Query
            using (MemberRepository memberRepository = new MemberRepository())
            {
                var member = memberRepository.GetSingle(query => query.Title == "Selected Member");

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

                memberRepository.Save();
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
