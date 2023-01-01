using BankContracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Manager
{
    public class JSONReader
    {
        public static List<User> ReadUsers()
        {
            string path = "..\\..\\users.json";

            List<User> users;

            using (StreamReader r = new StreamReader(path))
            {
                string file = r.ReadToEnd();
                users = JsonConvert.DeserializeObject<List<User>>(file);

                return users;
            }
        }
        public static User SaveUser(User user)
        {
            string file;
            List<User> users;

            string path = "..\\..\\users.json";

            if (File.Exists(path))
            {
                using (StreamReader r = new StreamReader(path))
                {
                    string fileTemp = r.ReadToEnd();
                    users = JsonConvert.DeserializeObject<List<User>>(fileTemp);
                    if (users != null)
                    {
                        foreach (User u in users)
                        {
                            if (u.Username == user.Username)
                            {
                                users.Remove(u);
                                break;
                            }
                        }
                    }
                    else
                    {
                        users = new List<User>();
                    }
                    users.Add(user);
                    file = JsonConvert.SerializeObject(users);
                }
            }
            else
            {
                users=new List<User>();
                users.Add(user);
                file = JsonConvert.SerializeObject(users);
            }

            File.WriteAllText(path, file);
            return user;
        }
    }
}
