using DiplomaTry2.Services;
using Microsoft.Data.SqlClient;
using Xunit;

namespace DiplomaTry2.ImportTests;

public sealed class ConnectionFormTests
{
    [Fact]
    public void New_connection_checks_certificate_even_if_previous_connection_trusted_it()
    {
        var form = new ConnectionForm("localhost", "printing", true, "", null);
        var cs = new SqlConnectionStringBuilder(ApplicationSettingsService.BuildConnectionString(form,
            "Server=localhost;Database=printing;Integrated Security=true;TrustServerCertificate=true"));
        Assert.False(cs.TrustServerCertificate);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, cs.Encrypt);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Certificate_choice_keeps_encryption_for_both_authentication_modes(bool integrated, bool trust)
    {
        var form = new ConnectionForm("localhost", "printing", integrated, "app", "test-password", trust);
        var cs = new SqlConnectionStringBuilder(ApplicationSettingsService.BuildConnectionString(form, ""));
        Assert.Equal(trust, cs.TrustServerCertificate);
        Assert.Equal(SqlConnectionEncryptOption.Mandatory, cs.Encrypt);
        Assert.Equal(integrated, cs.IntegratedSecurity);
        Assert.False(cs.PersistSecurityInfo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Changing_certificate_choice_preserves_password_for_same_sql_identity(bool trust)
    {
        var form = new ConnectionForm("localhost", "printing", false, "app", null, trust);
        var cs = new SqlConnectionStringBuilder(ApplicationSettingsService.BuildConnectionString(form,
            "Server=localhost;Database=printing;User ID=app;Password=saved-password"));
        Assert.Equal("saved-password", cs.Password);
        Assert.Equal(trust, cs.TrustServerCertificate);
        Assert.Throws<ArgumentException>(() => ApplicationSettingsService.BuildConnectionString(
            form with { Server = "other-server" }, cs.ConnectionString));
    }
}
