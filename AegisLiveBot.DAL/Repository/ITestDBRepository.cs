using AegisLiveBot.DAL.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository
{
    public interface ITestDBRepository : IRepository<TestDB>
    {
        TestDB GetByName(string name);
        bool AddOrUpdateByNameAndValue(string name, int value);
    }
}
