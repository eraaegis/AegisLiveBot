using AegisLiveBot.DAL.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AegisLiveBot.DAL.Repository.Implementation
{
    public class TestDBRepository : Repository<TestDB>, ITestDBRepository
    {
        public TestDBRepository(Context context) : base(context) { }
    }
}
