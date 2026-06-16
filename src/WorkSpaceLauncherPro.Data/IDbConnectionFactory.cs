using System.Data;

namespace WorkSpaceLauncherPro.Data;

public interface IDbConnectionFactory
{
    IDbConnection Create();
    string ConnectionString { get; }
}
