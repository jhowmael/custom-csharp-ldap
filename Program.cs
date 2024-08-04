using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Novell.Directory.Ldap;

class Program
{
    static void Main()
    {
        string ldapHost = "localhost";
        int ldapPort = 389;
        string adminUser = "cn=admin,dc=jonatanopenconsult,dc=com";

        Console.Write("Digite a senha do administrador LDAP: ");
        string adminPassword = Console.ReadLine(); // Recebe a senha do terminal

        string groupFolderPath = "Groups/Add";
        //string userFolderPath = "Users/Add";

        if (Directory.Exists(groupFolderPath))
        {
            string[] filePaths = Directory.GetFiles(groupFolderPath);
            string[] fileNames = Array.ConvertAll(filePaths, Path.GetFileName);

            foreach (var fileName in fileNames)
            {
                string filePath = Path.Combine(groupFolderPath, fileName);

                XDocument doc = XDocument.Load(filePath);

                var identificador = doc.Descendants("add-attr")
                    .Where(x => (string)x.Attribute("attr-name") == "Identificador")
                    .Select(x => (string)x.Element("value"))
                    .FirstOrDefault() ?? "default-identificador"; // Valor padrão se for nulo

                var descricao = doc.Descendants("add-attr")
                    .Where(x => (string)x.Attribute("attr-name") == "Descricao")
                    .Select(x => (string)x.Element("value"))
                    .FirstOrDefault() ?? "Sem descrição"; // Valor padrão se for nulo

                var membros = doc.Descendants("add-attr")
                    .Where(x => (string)x.Attribute("attr-name") == "Membros")
                    .Select(x => (string)x.Element("value"))
                    .Where(x => !string.IsNullOrEmpty(x)) // Filtra valores vazios
                    .ToArray(); // Converte para array

                string dn = $"cn={identificador},ou=Groups,dc=jonatanopenconsult,dc=com";

                var attributes = new LdapAttributeSet
                {
                    new LdapAttribute("objectClass", "top"),
                    new LdapAttribute("objectClass", "groupOfNames"),
                    new LdapAttribute("cn", identificador),
                    new LdapAttribute("description", descricao),
                };

                // Adiciona membros ao atributo "member"
                if (membros.Length > 0)
                {
                    attributes.Add(new LdapAttribute("member", membros));
                }
                else
                {
                    Console.WriteLine($"Erro: O grupo {identificador} deve conter pelo menos um membro.");
                    continue; // Pule para o próximo grupo
                }

                try
                {
                    using (var connection = new LdapConnection { SecureSocketLayer = false })
                    {
                        connection.Connect(ldapHost, ldapPort);
                        connection.Bind(adminUser, adminPassword);
                        try
                        {
                            var ldapEntry = connection.Read(dn);
                            Console.WriteLine($"A entrada com DN {dn} já existe.");
                        }
                        catch (LdapException ex)
                        {
                            if (ex.ResultCode == LdapException.NoSuchObject)
                            {
                                var ldapEntry = new LdapEntry(dn, attributes);
                                connection.Add(ldapEntry);
                                Console.WriteLine($"Entrada {dn} adicionada com sucesso.");
                            }
                            else
                            {
                                Console.WriteLine($"Erro ao verificar a entrada: {ex.Message}");
                            }
                        }
                    }
                }
                catch (LdapException e)
                {
                    Console.WriteLine($"Erro ao conectar ou adicionar entrada: {e.Message}");
                }

                Console.WriteLine($"Grupo {identificador} processado.");
            }
        }
        else
        {
            Console.WriteLine($"A pasta {groupFolderPath} não existe.");
        }
    }
}
