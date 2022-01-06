using GeniePlugin.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace InventoryView
{
    public class InventoryView : IPlugin
    {
        public static IHost _host;
        private Form _form;
        public static List<CharacterData> characterData = new List<CharacterData>();
        private static string basePath = Application.StartupPath;
        private string ScanMode;
        private int level = 1;
        private CharacterData currentData;
        private ItemData lastItem;
        private bool Debug;
        private string LastText = "";
        private bool _enabled = true;
        private string Place;

        public string Name => "Inventory View";

        public string Version => "1.8";

        public string Description => "Stores your character inventory and allows you to search items across characters.";

        public string Author => "Etherian <EtherianDR@gmail.com>";

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public void Initialize(IHost host)
        {
            InventoryView._host = host;
            InventoryView.basePath = InventoryView._host.get_Variable("PluginPath");
            InventoryView.LoadSettings(true);
        }

        public void Show()
        {
            if (_form == null || _form.IsDisposed)
                _form = (Form)new InventoryViewForm();
            _form.Show();
        }

        public void VariableChanged(string variable)
        {
        }

        public string ParseText(string text, string window)
        {
            if (ScanMode != null)
            {
                string input = text.Trim('\n', '\r', ' ');
                LastText = input;
                if ((!input.StartsWith("XML") || !input.EndsWith("XML")) && !string.IsNullOrEmpty(input))
                {
                    if (ScanMode == "Start")
                    {
                        if (input == "You have:")
                        {
                            InventoryView._host.EchoText("Scanning Inventory.");
                            ScanMode = "Inventory";
                            currentData = new CharacterData()
                            {
                                name = InventoryView._host.get_Variable("charactername"),
                                source = "Inventory"
                            };
                            InventoryView.characterData.Add(currentData);
                            level = 1;
                        }
                    }
                    else if (ScanMode == "Inventory")
                    {
                        if (!text.StartsWith("[Use INVENTORY HELP"))
                        {
                            if (text.StartsWith("Roundtime:"))
                            {
                                Match match = Regex.Match(input, "^Roundtime:\\s{1,3}(\\d{1,3})\\s{1,3}secs?\\.$");
                                ScanMode = "VaultStart";
                                InventoryView._host.EchoText(string.Format("Pausing {0} seconds for RT.", (object)int.Parse(match.Groups[1].Value)));
                                Thread.Sleep(int.Parse(match.Groups[1].Value) * 1000);
                                InventoryView._host.SendText("get my vault book");
                            }
                            else
                            {
                                int num = (text.Length - text.TrimStart(Array.Empty<char>()).Length + 1) / 3;
                                string str = input;
                                if (str.StartsWith("-"))
                                    str = str.Remove(0, 1);
                                if (str[str.Length - 1] == '.')
                                    str = str.TrimEnd('.');

                                str = Regex.Replace(str, @"^(an|a|some|several)\s", "");

                                if (num == 1)
                                    lastItem = currentData.AddItem(new ItemData()
                                    {
                                        tap = str
                                    });
                                else if (num == level)
                                    lastItem = lastItem.parent.AddItem(new ItemData()
                                    {
                                        tap = str
                                    });
                                else if (num == level + 1)
                                {
                                    lastItem = lastItem.AddItem(new ItemData()
                                    {
                                        tap = str
                                    });
                                }
                                else
                                {
                                    for (int index = num; index <= level; ++index)
                                        lastItem = lastItem.parent;
                                    lastItem = lastItem.AddItem(new ItemData()
                                    {
                                        tap = str
                                    });
                                }
                                level = num;
                            }
                        }
                    }
                    else if (ScanMode == "VaultStart")
                    {
                        if (Regex.Match(input, "^You get a.*vault book.*from").Success || input == "You are already holding that.")
                        {
                            Match match1 = Regex.Match(input, "^You get a.*vault book.*from.+your (.+)\\.");
                            Place = string.Format("{0}", match1.Groups[1]);
                            InventoryView._host.EchoText("Scanning Book Vault.");
                            InventoryView._host.SendText("read my vault book");
                        }
                        else if (input == "Vault Inventory:")
                        {
                            ScanMode = "Vault";
                            CharacterData characterData = new CharacterData();
                            characterData.name = InventoryView._host.get_Variable("charactername");
                            characterData.source = "Vault";
                            currentData = characterData;
                            InventoryView.characterData.Add(currentData);
                            level = 1;
                        }
                        else if (input == "What were you referring to?")
                        {
                            InventoryView._host.EchoText("Skipping Book Vault.");
                            ScanMode = "StandardStart";
                            InventoryView._host.SendText("vault standard");
                        }
                        else if (input == "The script that the vault book is written in is unfamiliar to you.  You are unable to read it." || input == "The vault book is filled with blank pages pre-printed with branch office letterhead.  An advertisement touting the services of Rundmolen Bros. Storage Co. is pasted on the inside cover." || input == "You currently do not have a vault rented.")
                        {
                            InventoryView._host.EchoText("Skipping Book Vault.");
                            InventoryView._host.SendText("put my vault book in my " + Place);
                            ScanMode = "StandardStart";
                            InventoryView._host.SendText("vault standard");
                        }
                    }
                    else if (ScanMode == "Vault")
                    {
                        if (text.StartsWith("The last note in your book indicates that your vault contains"))
                        {
                            InventoryView._host.SendText("put my vault book in my " + Place);
                            ScanMode = "FamilyStart";
                            InventoryView._host.SendText("vault family");

                        }
                        else
                        {
                            int num1 = text.Length - text.TrimStart(Array.Empty<char>()).Length;
                            int num2 = 1;
                            if (num1 > 4)
                                num2 += (num1 - 4) / 2;
                            string str = input;
                            if (str.StartsWith("-"))
                                str = str.Remove(0, 1);
                            if (str[str.Length - 1] == '.')
                                str = str.TrimEnd('.');

                            str = Regex.Replace(str, @"^(an|a|some|several)\s", "");
                            if (num2 == 1)
                                lastItem = currentData.AddItem(new ItemData()
                                {
                                    tap = str,
                                    storage = true
                                });
                            else if (num2 == level)
                                lastItem = lastItem.parent.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            else if (num2 == level + 1)
                            {
                                lastItem = lastItem.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            }
                            else
                            {
                                for (int index = num2; index <= level; ++index)
                                    lastItem = lastItem.parent;
                                lastItem = lastItem.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            }
                            level = num2;
                        }
                    }

                    else if (ScanMode == "StandardStart")
                    {
                        if (Regex.Match(input, "^You flag down a local you know works with the Estate Holders' Council and send \\w+ to the nearest carousel.").Success || input == "You are already holding that.")
                        {
                            InventoryView._host.EchoText("Scanning Vault.");
                        }
                        else if (input == "Vault Inventory:")
                        {
                            ScanMode = "Standard";
                            CharacterData characterData = new CharacterData();
                            characterData.name = InventoryView._host.get_Variable("charactername");
                            characterData.source = "Vault";
                            currentData = characterData;
                            InventoryView.characterData.Add(currentData);
                            level = 1;
                        }
                        else if (input == "You currently do not have access to VAULT STANDARD or VAULT FAMILY.  You will need to use VAULT PAY CONVERT to convert an urchin runner for this purpose." || input == "You currently have no contract with the representative of the local Traders' Guild for this service." || input == "You have no arrangements with the local Traders' Guild representative for urchin runners." || input == "You can't access your vault at this time." || input == "You currently do not have a vault rented.")
                        {
                            InventoryView._host.EchoText("Skipping Standard Vault.");
                            ScanMode = "FamilyStart";
                            InventoryView._host.SendText("vault family");
                        }
                    }
                    else if (ScanMode == "Standard")
                    {
                        if (text.StartsWith("The last note indicates that your vault contains"))
                        {
                            ScanMode = "FamilyStart";
                        }
                        else
                        {
                            int num1 = text.Length - text.TrimStart().Length;
                            int num2 = 1;
                            switch (num1)
                            {
                                case 5:
                                    num2 = 1;
                                    break;
                                case 10:
                                    num2 = 2;
                                    break;
                                case 15:
                                    num2 = 3;
                                    break;
                                case 18:
                                    num2 = 4;
                                    break;
                            }
                            string str = input;

                            if (str.StartsWith("-"))
                                str = str.Remove(0, 1);
                            if (str.StartsWith(" "))
                                str = str.Remove(0, 1);
                            if (str[str.Length - 1] == '.')
                                str = str.TrimEnd('.');
                            str = Regex.Replace(str, @"\)\s(an|a|some|several)\s", ") ");
                            str = Regex.Replace(str, @"\)\s{2}(an|a|some|several)\s", ") ");
                            str = Regex.Replace(str, @"^(an|a|some|several)\s", "");
                            if (num2 == 1)
                                lastItem = currentData.AddItem(new ItemData()
                                {
                                    tap = str,
                                    storage = true
                                });
                            else if (num2 == level)
                                lastItem = lastItem.parent.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            else if (num2 == level + 1)
                            {
                                lastItem = lastItem.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            }
                            else
                            {
                                for (int index = num2; index <= level; ++index)
                                    lastItem = lastItem.parent;
                                lastItem = lastItem.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            }
                            level = num2;
                        }
                    }

                    else if (ScanMode == "FamilyStart")
                    {
                        if (text.StartsWith("Roundtime:"))
                        {
                            Match match = Regex.Match(input, "^Roundtime:\\s{1,3}(\\d{1,3})\\s{1,3}secs?\\.$");
                            InventoryView._host.EchoText(string.Format("Pausing {0} seconds for RT.", (object)int.Parse(match.Groups[1].Value)));
                            Thread.Sleep(int.Parse(match.Groups[1].Value) * 1000);
                            ScanMode = "FamilyStart";
                            InventoryView._host.SendText("vault family");
                        }
                        if (Regex.Match(input, "You flag down an urchin and direct him to the nearest carousel").Success || input == "You flag down an urchin and direct her to the nearest carousel")
                        {
                            InventoryView._host.EchoText("Scanning Family Vault.");
                        }
                        else if (input == "Vault Inventory:")
                        {
                            ScanMode = "FamilyVault";
                            currentData = new CharacterData()
                            {
                                name = InventoryView._host.get_Variable("charactername"),
                                source = "Family Vault"
                            };
                            InventoryView.characterData.Add(currentData);
                            level = 1;
                        }
                        else if (input == "You currently do not have access to VAULT STANDARD or VAULT FAMILY.  You will need to use VAULT PAY CONVERT to convert an urchin runner for this purpose." || input == "You have no arrangements with the local Traders' Guild representative for urchin runners." || input == "You currently have no contract with the representative of the local Traders' Guild for this service." || input == "Now may not be the best time for that." || input == "You look around, but cannot find a nearby urchin to send to effect the transfer." || input == "You can't access the family vault at this time." || input == "You can't access your vault at this time." || input == "You currently do not have a vault rented.")
                        {
                            if (text.StartsWith("Roundtime:"))
                            {
                                Match match = Regex.Match(input, "^Roundtime:\\s{1,3}(\\d{1,3})\\s{1,3}secs?\\.$");
                                InventoryView._host.EchoText(string.Format("Pausing {0} seconds for RT.", (object)int.Parse(match.Groups[1].Value)));
                                Thread.Sleep(int.Parse(match.Groups[1].Value) * 1000);
                                ScanMode = "DeedStart";
                                InventoryView._host.SendText("get my deed register");
                            }
                            InventoryView._host.EchoText("Skipping Family Vault.");
                            ScanMode = "DeedStart";
                            InventoryView._host.SendText("get my deed register");
                        }
                    }
                    else if (ScanMode == "FamilyVault")
                    {
                        if (text.StartsWith("The last note indicates that your vault contains"))
                        {
                            ScanMode = "DeedStart";
                        }
                        else
                        {
                            int num1 = text.Length - text.TrimStart(Array.Empty<char>()).Length;
                            int num2 = 1;
                            if (num1 > 4)
                                num2 += (num1 - 4) / 2;
                            switch (num1)
                            {
                                case 5:
                                    num2 = 1;
                                    break;
                                case 10:
                                    num2 = 2;
                                    break;
                                case 15:
                                    num2 = 3;
                                    break;
                                case 18:
                                    num2 = 4;
                                    break;
                            }
                            string str = input;

                            if (str.StartsWith("-"))
                                str = str.Remove(0, 1);
                            if (str.StartsWith(" "))
                                str = str.Remove(0, 1);
                            if (str[str.Length - 1] == '.')
                                str = str.TrimEnd('.');
                            str = Regex.Replace(str, @"\)\s(an|a|some|several)\s", ") ");
                            str = Regex.Replace(str, @"\)\s{2}(an|a|some|several)\s", ") ");
                            str = Regex.Replace(str, @"^(an|a|some|several)\s", "");
                            if (num2 == 1)
                                lastItem = currentData.AddItem(new ItemData()
                                {
                                    tap = str,
                                    storage = true
                                });
                            else if (num2 == level)
                                lastItem = lastItem.parent.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            else if (num2 == level + 1)
                            {
                                lastItem = lastItem.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            }
                            else
                            {
                                for (int index = num2; index <= level; ++index)
                                    lastItem = lastItem.parent;
                                lastItem = lastItem.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            }
                            level = num2;
                        }
                    }
                    else if (ScanMode == "DeedStart")
                    {
                        if (text.StartsWith("Roundtime:"))
                        {
                            Match match = Regex.Match(input, "^Roundtime:\\s{1,3}(\\d{1,3})\\s{1,3}secs?\\.$");
                            InventoryView._host.EchoText(string.Format("Pausing {0} seconds for RT.", (object)int.Parse(match.Groups[1].Value)));
                            Thread.Sleep(int.Parse(match.Groups[1].Value) * 1000);
                            ScanMode = "DeedStart";
                            InventoryView._host.SendText("get my deed register");
                        }

                        if (Regex.Match(input, "^You get a.*deed register.*from").Success || input == "You are already holding that.")
                        {

                            Match match1 = Regex.Match(input, "^You get a.*deed register.*from.+your (.+)\\.");
                            Place = string.Format("{0}", match1.Groups[1]);
                            InventoryView._host.EchoText("Scanning Deed Register.");
                            InventoryView._host.SendText("turn my deed register to contents");
                            InventoryView._host.SendText("read my deed register");
                        }
                        else if (input == "Page -- Deed")
                        {
                            ScanMode = "Deed";
                            currentData = new CharacterData()
                            {
                                name = InventoryView._host.get_Variable("charactername"),
                                source = "Deed"
                            };
                            InventoryView.characterData.Add(currentData);
                            level = 1;
                        }
                        else if (input == "What were you referring to?")
                        {
                            InventoryView._host.EchoText("Skipping Deed Register.");
                            ScanMode = "HomeStart";
                            InventoryView._host.SendText("home recall");
                        }
                        else if (Regex.Match(input, "^You haven't stored any deeds in this register\\.  It can hold \\d+ deeds in total\\.").Success || input == "You shouldn't do that to somebody eles's deed book." || input == "You shouldn't read somebody else's deed book.")
                        {
                            InventoryView._host.SendText("put my deed register in my " + Place);
                            ScanMode = "HomeStart";
                            InventoryView._host.SendText("home recall");
                        }

                    }
                    else if (ScanMode == "Deed")
                    {
                        if (input.Contains("Currently stored"))
                        {
                            InventoryView._host.SendText("put my deed register in my " + Place);
                            ScanMode = "HomeStart";
                            InventoryView._host.SendText("home recall");
                        }
                        else
                        {
                            string str = Regex.Replace(input, @"^(\d+\s--\s)(an|a|some|several)\s", "");
                            if (str[str.Length - 1] == '.')
                                str = str.TrimEnd('.');

                            lastItem = currentData.AddItem(new ItemData()
                            {
                                tap = str,
                                storage = false
                            });
                        }
                    }
                    else if (ScanMode == "HomeStart")
                    {
                        if (input == "The home contains:")
                        {
                            InventoryView._host.EchoText("Scanning Home.");
                            ScanMode = "Home";
                            currentData = new CharacterData()
                            {
                                name = InventoryView._host.get_Variable("charactername"),
                                source = "Home"
                            };
                            InventoryView.characterData.Add(currentData);
                            level = 1;
                        }
                        else if (input.StartsWith("Your documentation filed with the Estate Holders"))
                        {
                            InventoryView._host.EchoText("Skipping Home.");
                            if (InventoryView._host.get_Variable("guild") == "Trader")
                            {
                                ScanMode = "TraderStart";
                                InventoryView._host.SendText("get my storage book");
                            }
                            else
                            {
                                ScanMode = (string)null;
                                InventoryView._host.EchoText("Scan Complete.");
                                InventoryView._host.SendText("#parse InventoryView scan complete");
                                InventoryView.SaveSettings();
                            }
                        }
                        else if (input == "You shouldn't do that while inside of a home.  Step outside if you need to check something.")
                        {
                            InventoryView._host.EchoText("You cannot check the contents of your home while inside of a home. Step outside and try again.");
                            if (InventoryView._host.get_Variable("guild") == "Trader")
                            {
                                ScanMode = "TraderStart";
                                InventoryView._host.SendText("get my storage book");
                            }
                            else
                            {
                                ScanMode = (string)null;
                                InventoryView._host.EchoText("Scan Complete.");
                                InventoryView._host.SendText("#parse InventoryView scan complete");
                                InventoryView.SaveSettings();
                            }
                        }
                    }
                    else if (ScanMode == "Home")
                    {
                        if (input == ">")
                        {
                            if (InventoryView._host.get_Variable("guild") == "Trader")
                            {
                                ScanMode = "TraderStart";
                                InventoryView._host.SendText("get my storage book");
                            }
                            else
                            {
                                ScanMode = (string)null;
                                InventoryView._host.EchoText("Scan Complete.");
                                InventoryView._host.SendText("#parse InventoryView scan complete");
                                InventoryView.SaveSettings();
                            }
                        }
                        else if (input.StartsWith("Attached:"))
                        {
                            string str = input.Replace("Attached: ", "");
                            if (str[str.Length - 1] == '.')
                                str = str.TrimEnd('.');
                            str = Regex.Replace(str, @"^(an|a|some|several)\s", "");
                            lastItem = (lastItem.parent != null ? lastItem.parent : lastItem).AddItem(new ItemData()
                            {
                                tap = str
                            });
                        }
                        else
                        {
                            string str = input.Substring(input.IndexOf(":") + 2);
                            if (str[str.Length - 1] == '.')
                                str = str.TrimEnd('.');

                            str = Regex.Replace(str, @"^(an|a|some|several)\s", "");

                            lastItem = currentData.AddItem(new ItemData()
                            {
                                tap = str,
                                storage = true
                            });
                        }
                    }
                    else if (ScanMode == "TraderStart")
                    {
                        if (Regex.Match(input, "^You get a.*storage book.*from").Success || input == "You are already holding that.")
                        {
                            Match match1 = Regex.Match(input, "^You get a.*storage book.*from.+your (.+)\\.");
                            Place = string.Format("{0}", match1.Groups[1]);

                            InventoryView._host.EchoText("Scanning Trader Storage.");
                            InventoryView._host.SendText("read my storage book");
                        }
                        else if (input == "in the known realms since 402.")
                        {
                            ScanMode = "Trader";
                            currentData = new CharacterData()
                            {
                                name = InventoryView._host.get_Variable("charactername"),
                                source = "TraderStorage"
                            };
                            InventoryView.characterData.Add(currentData);
                            level = 1;
                        }
                        else if (input == "What were you referring to?" || input == "The storage book is filled with complex lists of inventory that make little sense to you.")
                        {
                            ScanMode = (string)null;
                            InventoryView._host.EchoText("Skipping Trader Storage.");
                            InventoryView._host.EchoText("Scan Complete.");
                            InventoryView._host.SendText("#parse InventoryView scan complete");
                            InventoryView.SaveSettings();
                        }
                    }
                    else if (ScanMode == "Trader")
                    {
                        if (text.StartsWith("A notation at the bottom indicates") || text.StartsWith("...You have nothing currently stored."))
                        {
                            ScanMode = (string)null;
                            InventoryView._host.SendText("put my storage book in my " + Place);
                            InventoryView._host.EchoText("Scan Complete.");
                            InventoryView._host.SendText("#parse InventoryView scan complete");
                            InventoryView.SaveSettings();
                        }
                        else
                        {
                            int num = (text.Length - text.TrimStart().Length + 1) / 3;

                            string str = input;
                            if (str.StartsWith("-"))
                                str = str.Remove(0, 1);

                            str = Regex.Replace(str, @"^(an|a|some|several)\s", "");

                            if (str[str.Length - 1] == '.')
                                str = str.TrimEnd('.');
                            if (num == 1)
                                lastItem = currentData.AddItem(new ItemData()
                                {
                                    tap = str,
                                    storage = true
                                });
                            else if (num == level)
                                lastItem = lastItem.parent.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            else if (num == level + 1)
                            {
                                lastItem = lastItem.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            }
                            else
                            {
                                for (int index = num; index <= level; ++index)
                                    lastItem = lastItem.parent;
                                lastItem = lastItem.AddItem(new ItemData()
                                {
                                    tap = str
                                });
                            }
                            level = num;
                        }
                    }
                }
            }
            return text;
        }

        public string ParseInput(string text)
        {
            if (!text.ToLower().StartsWith("/inventoryview ") && !text.ToLower().StartsWith("/iv "))
                return text;
            string[] strArray = text.Split(' ');
            if (strArray.Length == 1 || strArray[1].ToLower() == "help")
                Help();
            else if (strArray[1].ToLower() == "scan")
            {
                if (InventoryView._host.get_Variable("connected") == "0")
                {
                    InventoryView._host.EchoText("You must be connected to the server to do a scan.");
                }
                else
                {
                    InventoryView.LoadSettings();
                    ScanMode = "Start";
                    while (InventoryView.characterData.Where<CharacterData>((Func<CharacterData, bool>)(tbl => tbl.name == InventoryView._host.get_Variable("charactername"))).Count<CharacterData>() > 0)
                        InventoryView.characterData.Remove(InventoryView.characterData.Where<CharacterData>((Func<CharacterData, bool>)(tbl => tbl.name == InventoryView._host.get_Variable("charactername"))).First<CharacterData>());
                    InventoryView._host.SendText("inventory list");
                }
            }
            else if (strArray[1].ToLower() == "open")
                Show();
            else if (strArray[1].ToLower() == "debug")
            {
                Debug = !Debug;
                InventoryView._host.EchoText("InventoryView Debug Mode " + (Debug ? "ON" : "OFF"));
            }
            else if (strArray[1].ToLower() == "lasttext")
            {
                Debug = !Debug;
                InventoryView._host.EchoText("InventoryView Debug Last Text: " + LastText);
            }
            else
                Help();
            return string.Empty;
        }

        public void Help()
        {
            InventoryView._host.EchoText("Inventory View plugin options:");
            InventoryView._host.EchoText("/InventoryView scan  -- scan the items on the current character.");
            InventoryView._host.EchoText("/InventoryView open  -- open the InventoryView Window to see items.");
        }

        public static void RemoveParents(List<ItemData> iList)
        {
            foreach (ItemData i in iList)
            {
                i.parent = (ItemData)null;
                InventoryView.RemoveParents(i.items);
            }
        }

        public static void AddParents(List<ItemData> iList, ItemData parent)
        {
            foreach (ItemData i in iList)
            {
                i.parent = parent;
                InventoryView.AddParents(i.items, i);
            }
        }

        public void ParseXML(string xml)
        {
        }

        public void ParentClosing()
        {
        }

        public static void LoadSettings(bool initial = false)
        {
            string path = Path.Combine(InventoryView.basePath, "InventoryView.xml");
            if (!File.Exists(path))
                return;
            try
            {
                using (Stream stream = (Stream)File.Open(path, FileMode.Open))
                    InventoryView.characterData = (List<CharacterData>)new XmlSerializer(typeof(List<CharacterData>)).Deserialize(stream);
                foreach (CharacterData characterData in InventoryView.characterData)
                    InventoryView.AddParents(characterData.items, (ItemData)null);
                if (initial)
                    return;
                InventoryView._host.EchoText("InventoryView data loaded.");
            }
            catch (IOException ex)
            {
                InventoryView._host.EchoText("Error reading InventoryView file: " + ex.Message);
            }
        }

        public static void SaveSettings()
        {
            string path = Path.Combine(InventoryView.basePath, "InventoryView.xml");
            try
            {
                foreach (CharacterData characterData in InventoryView.characterData)
                    InventoryView.RemoveParents(characterData.items);
                FileStream fileStream = new FileStream(path, FileMode.Create);
                new XmlSerializer(typeof(List<CharacterData>)).Serialize((Stream)fileStream, (object)InventoryView.characterData);
                fileStream.Close();
                foreach (CharacterData characterData in InventoryView.characterData)
                    InventoryView.AddParents(characterData.items, (ItemData)null);
            }
            catch (IOException ex)
            {
                InventoryView._host.EchoText("Error writing to InventoryView file: " + ex.Message);
            }
        }
    }
}