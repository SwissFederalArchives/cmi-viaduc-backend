using System;
using System.Collections.Generic;

namespace CMI.Engine.Security.Tests
{
    /// <summary>
    ///     Simple testing suite for integration tests
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            var pwHelper = new PasswordHelper("JustSomethingStupid");
            var allPasswords = new List<IdPassword>();
            var passwords = new Dictionary<string, string>();
            var itemCount = 10000000;
            for (var i = 0; i < itemCount; i++)
            {
                var pwd = pwHelper.GetHashPassword(i.ToString());
                if (passwords.ContainsKey(pwd))
                {
                    Console.WriteLine($"Duplicate passwords for id's {i} and {passwords[pwd]}");
                }
                else
                {
                    passwords.Add(pwd, i.ToString());
                }

                allPasswords.Add(new IdPassword {Id = i, Password = pwd});
            }

            // 
            Console.WriteLine($"Created {itemCount} passwords.");

            for (var index = 1; index <= 25; index++)
            {
                var idPassword = allPasswords[index];
                Console.WriteLine($"{idPassword.Id.ToString().PadLeft(5, ' ')}: {idPassword.Password}");
            }

            // Check if we can create the same again
            foreach (var idPassword in allPasswords)
            {
                if (idPassword.Password != pwHelper.GetHashPassword(idPassword.Id.ToString()))
                {
                    Console.WriteLine("Help! it doesn't work");
                    break;
                }
            }

            Console.WriteLine("Finished checking passwords");
            Console.ReadLine();
        }
    }

    internal class IdPassword
    {
        public int Id { get; set; }
        public string Password { get; set; }
    }
}