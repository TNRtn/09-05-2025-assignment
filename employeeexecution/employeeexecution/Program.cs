using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EmployeeManagement
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime JoinDate { get; set; }
    }

    public class EmployeeService
    {
        private readonly string _dbConnection;

        public EmployeeService(string connectionString)
        {
            _dbConnection = connectionString;
        }

        public async Task<List<Employee>> GetRecentHiresAsync()
        {
            var recentEmployees = new List<Employee>();
            string query = "SELECT Id, FullName, JoiningDate FROM Employees WHERE JoiningDate >= DATEADD(MONTH, -6, GETDATE())";

            using (var connection = new SqlConnection(_dbConnection))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        recentEmployees.Add(new Employee
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            JoinDate = reader.GetDateTime(2)
                        });
                    }
                }
            }

            return recentEmployees;
        }

        public List<Employee> GetRecentHires()
        {
            var recentEmployees = new List<Employee>();
            string query = "SELECT Id, FullName, JoiningDate FROM Employees WHERE JoiningDate >= DATEADD(MONTH, -6, GETDATE())";

            using (var connection = new SqlConnection(_dbConnection))
            {
                connection.Open();

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        recentEmployees.Add(new Employee
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            JoinDate = reader.GetDateTime(2)
                        });
                    }
                }
            }

            return recentEmployees;
        }

        public async Task<DateTime> GetDatabaseTimeAsync()
        {
            using (var connection = new SqlConnection(_dbConnection))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT GETDATE()", connection))
                {
                    return (DateTime)await command.ExecuteScalarAsync();
                }
            }
        }
    }

    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Employee Data Retrieval Comparison\n");

            var service = new EmployeeService(
                "Server=LAPTOP-2IHUF3HB\\SQLEXPRESS;Database=assignment;Trusted_Connection=True");

            // Regular method
            Console.WriteLine("Running standard database query...");
            var timer = Stopwatch.StartNew();
            var employees = service.GetRecentHires();
            timer.Stop();

            Console.WriteLine($"Found {employees.Count} employees hired in last 6 months");
            ShowSampleEmployees(employees);
            Console.WriteLine($"Completed in {timer.ElapsedMilliseconds} ms\n");

            // Async method
            Console.WriteLine("Running async database query...");
            timer.Restart();
            var asyncEmployees = await service.GetRecentHiresAsync();
            timer.Stop();

            Console.WriteLine($"Found {asyncEmployees.Count} employees hired in last 6 months");
            ShowSampleEmployees(asyncEmployees);
            Console.WriteLine($"Completed in {timer.ElapsedMilliseconds} ms\n");

            // Show current database time
            await ShowDatabaseTime(service);
        }

        static void ShowSampleEmployees(List<Employee> employees)
        {
            if (employees.Count == 0)
            {
                Console.WriteLine("No recent hires found");
                return;
            }

            Console.WriteLine("Sample employees:");
            for (int i = 0; i < Math.Min(3, employees.Count); i++)
            {
                Console.WriteLine($"- {employees[i].Name} (ID: {employees[i].Id}, Joined: {employees[i].JoinDate:d})");
            }
        }

        static async Task ShowDatabaseTime(EmployeeService service)
        {
            try
            {
                Console.WriteLine("Checking database server time...");
                var dbTime = await service.GetDatabaseTimeAsync();
                Console.WriteLine($"Database server time is {dbTime:MMMM dd, yyyy hh:mm:ss tt}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Couldn't get database time: {ex.Message}");
            }
        }
    }
}