using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IConfiguration _config;

    public TestController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("sql")]
    public IActionResult TestSql()
    {
        var connStr = _config.GetConnectionString("DefaultConnection");

        using SqlConnection conn = new SqlConnection(connStr);
        conn.Open();

        return Ok("✅ SQL SERVER KẾT NỐI OK");
    }
}
