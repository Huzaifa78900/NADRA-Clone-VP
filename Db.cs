using System.Data;
using Microsoft.Data.Sqlite;

namespace NadraApp;

/// <summary>Tiny SQLite data-access layer for the NADRA citizen registry.</summary>
static class Db
{
    static readonly string Cs = "Data Source=" + Path.Combine(AppContext.BaseDirectory, "nadra.db");
    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "nadra.db");

    public static void Init()
    {
        Exec(@"CREATE TABLE IF NOT EXISTS Citizens(
            Id INTEGER PRIMARY KEY AUTOINCREMENT, CNIC TEXT UNIQUE NOT NULL, FullName TEXT NOT NULL,
            FatherName TEXT, Gender TEXT, DOB TEXT, Marital TEXT, BloodGroup TEXT, Province TEXT, City TEXT,
            Address TEXT, Phone TEXT, Family INTEGER, Disabled INTEGER, Overseas INTEGER,
            Services TEXT, Photo TEXT, Created TEXT)");

        if (Scalar("SELECT COUNT(*) FROM Citizens") == 0)
        {
            var seed = new (string cnic, string name, string father, string g, string dob, string m, string bg, string prov, string city, string ph)[]
            {
                ("35202-1234567-1","Ali Raza","Muhammad Aslam","Male","1990-04-12","Married","B+","Punjab","Lahore","0300-1234567"),
                ("37405-7654321-2","Ayesha Khan","Imran Khan","Female","1995-09-23","Single","O+","Punjab","Rawalpindi","0321-7654321"),
                ("42101-1112223-3","Bilal Ahmed","Tariq Ahmed","Male","1988-01-30","Married","A+","Sindh","Karachi","0333-1112223"),
                ("42301-9988776-4","Sana Malik","Naeem Malik","Female","1992-12-05","Married","AB+","Sindh","Hyderabad","0345-9988776"),
                ("17301-5566778-5","Hamza Yousafzai","Gul Rahman","Male","1985-07-19","Married","O-","Khyber Pakhtunkhwa","Peshawar","0311-5566778"),
                ("54401-3344556-6","Noor Baloch","Abdul Karim","Male","1999-03-14","Single","B-","Balochistan","Quetta","0312-3344556"),
                ("61101-2468135-7","Fatima Noor","Noor Muhammad","Female","2001-06-28","Single","A-","Islamabad Capital Territory","Islamabad","0301-2468135"),
                ("35201-1357924-8","Usman Tariq","Tariq Mehmood","Male","1978-11-02","Married","A+","Punjab","Lahore","0322-1357924"),
            };
            foreach (var s in seed)
                Exec(@"INSERT INTO Citizens(CNIC,FullName,FatherName,Gender,DOB,Marital,BloodGroup,Province,City,Address,Phone,Family,Disabled,Overseas,Services,Photo,Created)
                       VALUES(@c,@n,@f,@g,@d,@m,@b,@p,@ci,@a,@ph,@fm,0,0,'Smart Card',NULL,@cr)",
                    ("@c", s.cnic), ("@n", s.name), ("@f", s.father), ("@g", s.g), ("@d", s.dob), ("@m", s.m),
                    ("@b", s.bg), ("@p", s.prov), ("@ci", s.city), ("@a", "House 1, Street 5, " + s.city),
                    ("@ph", s.ph), ("@fm", 3), ("@cr", DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
        }
    }

    public static DataTable Query(string sql, params (string k, object v)[] ps)
    {
        using var c = new SqliteConnection(Cs); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        foreach (var (k, v) in ps) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
        var dt = new DataTable();
        using var r = cmd.ExecuteReader(); dt.Load(r);
        return dt;
    }

    public static int Exec(string sql, params (string k, object v)[] ps)
    {
        using var c = new SqliteConnection(Cs); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        foreach (var (k, v) in ps) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    public static long Scalar(string sql, params (string k, object v)[] ps)
    {
        using var c = new SqliteConnection(Cs); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        foreach (var (k, v) in ps) cmd.Parameters.AddWithValue(k, v ?? DBNull.Value);
        var o = cmd.ExecuteScalar();
        return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o);
    }
}
