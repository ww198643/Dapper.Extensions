﻿using System;
using System.Data.SQLite;

namespace Dapper.Extensions.SQLite
{
    public class SQLiteDapper : BaseDapper<SQLiteConnection>
    {
        public SQLiteDapper(IServiceProvider service, string connectionName = "DefaultConnection", bool enableMasterSlave = false, bool readOnly = false) : base(service, connectionName, enableMasterSlave, readOnly)
        {
        }
    }
}
