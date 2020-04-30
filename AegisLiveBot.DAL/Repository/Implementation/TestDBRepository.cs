using AegisLiveBot.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class TestDBRepository : Repository<TestDB>, ITestDBRepository
    {
        public TestDBRepository(Context context) : base(context) { }
        public TestDB GetByName(string name)
        {
            return _dbset.FirstOrDefault(x => x.Name == name);
        }
        public bool AddOrUpdateByNameAndValue(string name, int value)
        {
            var testDB = GetByName(name);
            if(testDB == null)
            {
                _dbset.Add(new TestDB { Name = name, Value = value });
                return true;
            } else
            {
                testDB.Value = value;
                Update(testDB);
                return false;
            }
        }
    }
}
