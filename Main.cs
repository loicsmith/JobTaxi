using Life;
using Life.BizSystem;
using Life.Network;
using Life.UI;
using Mirror;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Internal;
using ModKit.ORM;
using MODRP_JobTaxi.Classes;
using MODRP_JobTaxi.Functions;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;
using System.IO;
using _menu = AAMenu.Menu;

namespace MODRP_JobTaxi.Main
{

    class Main : ModKit.ModKit
    {
        public CheckPointCreator LineCreator = new CheckPointCreator();
        public CheckPointPlayable LinePlayable = new CheckPointPlayable();

        public static string ConfigDirectoryPath;
        public static string ConfigJobTaxiPath;
        public static JobTaxiConfig _JobTaxiConfig;

        public Main(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Loicsmith");

            LineCreator.Context = this;
            LinePlayable.Context = this;
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");

            InitAAmenu();

            Orm.RegisterTable<OrmManager.JobTaxi_JobTaxiManager>();

            InitConfig();
            _JobTaxiConfig = LoadConfigFile(ConfigJobTaxiPath);

            Nova.server.OnPlayerDisconnectEvent += (NetworkConnection conn) =>
            {
                LinePlayable.RemoveNetIDFromList(conn.connectionId);
            };

        }


        private void InitConfig()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/JobTaxi";
                ConfigJobTaxiPath = Path.Combine(ConfigDirectoryPath, "JobTaxiConfig.json");

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
                if (!File.Exists(ConfigJobTaxiPath)) InitJobTaxiConfig();
            }
            catch (IOException ex)
            {
                Logger.LogError("InitDirectory", ex.Message);
            }
        }

        private void InitJobTaxiConfig()
        {
            JobTaxiConfig JobTaxiConfig = new JobTaxiConfig();
            string json = JsonConvert.SerializeObject(JobTaxiConfig, Formatting.Indented);
            File.WriteAllText(ConfigJobTaxiPath, json);
        }

        private JobTaxiConfig LoadConfigFile(string path)
        {
            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                JobTaxiConfig JobTaxiConfig = JsonConvert.DeserializeObject<JobTaxiConfig>(jsonContent);

                return JobTaxiConfig;
            }
            else return null;
        }

        private void SaveConfig(string path)
        {
            string json = JsonConvert.SerializeObject(_JobTaxiConfig, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public void ConfigEditor(Player player)
        {
            Panel panel = PanelHelper.Create("JobTaxi | Config JSON", UIPanel.PanelType.TabPrice, player, () => ConfigEditor(player));

            panel.AddTabLine($"{TextFormattingHelper.Color("CityHallId : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobTaxiConfig.CityHallId}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "CityHallId");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("TaxPercentage : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobTaxiConfig.TaxPercentage}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "TaxPercentage");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("PlayerReceivePercentage : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobTaxiConfig.PlayerReceivePercentage}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "PlayerReceivePercentage");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("MinMoneyPerCustomer : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobTaxiConfig.MinMoneyPerCustomer}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "MinMoneyPerCustomer");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("MaxMoneyPerCustomer : ", TextFormattingHelper.Colors.Info)}" + $"{TextFormattingHelper.Color($"{_JobTaxiConfig.MaxMoneyPerCustomer}", TextFormattingHelper.Colors.Verbose)}", _ =>
            {
                EditLineInConfig(player, "MaxMoneyPerCustomer");
            });
            panel.AddTabLine($"{TextFormattingHelper.Color("Appliquer la configuration", TextFormattingHelper.Colors.Success)}", _ =>
            {
                SaveConfig(ConfigJobTaxiPath);
                panel.Refresh();
            });

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.AdminPluginPanel(player));
            panel.CloseButton();
            panel.Display();
        }

        public void EditLineInConfig(Player player, string Param)
        {
            Panel panel = PanelHelper.Create("JobTaxi | Edit JSON", UIPanel.PanelType.Input, player, () => EditLineInConfig(player, Param));
            panel.TextLines.Add($"Modification de la valeur de : \"{Param}\"");
            panel.SetInputPlaceholder("Veuillez saisir une valeur");
            panel.AddButton("Valider", (ui) =>
            {
                string input = ui.inputText;

                switch (Param)
                {
                    case "CityHallId":
                        // int
                        if (int.TryParse(input, out int valueCity))
                        {
                            _JobTaxiConfig.CityHallId = valueCity;
                        }
                        else
                        {
                            player.Notify("JobTaxi", "Veuillez saisir un nombre entier.", NotificationManager.Type.Error);
                        }
                        break;
                    case "TaxPercentage":
                        // double
                        if (float.TryParse(input, out float valueTax))
                        {
                            _JobTaxiConfig.TaxPercentage = valueTax;
                        }
                        else
                        {
                            player.Notify("JobTaxi", "Veuillez saisir un nombre valide.", NotificationManager.Type.Error);
                        }
                        break;
                    case "PlayerReceivePercentage":
                        // double
                        if (float.TryParse(input, out float valueTaxPlayer))
                        {
                            _JobTaxiConfig.PlayerReceivePercentage = valueTaxPlayer;
                        }
                        else
                        {
                            player.Notify("JobTaxi", "Veuillez saisir un nombre valide.", NotificationManager.Type.Error);
                        }
                        break;
                   
                    case "MinMoneyPerCustomer":
                        //float
                        if (float.TryParse(input, out float valueMinMoney))
                        {
                            _JobTaxiConfig.MinMoneyPerCustomer = valueMinMoney;
                        }
                        else
                        {
                            player.Notify("JobTaxi", "Veuillez saisir un nombre valide .", NotificationManager.Type.Error);
                        }
                        break;
                    case "MaxMoneyPerCustomer":
                        //float
                        if (float.TryParse(input, out float valueMaxMoney))
                        {
                            _JobTaxiConfig.MaxMoneyPerCustomer = valueMaxMoney;
                        }
                        else
                        {
                            player.Notify("JobTaxi", "Veuillez saisir un nombre valide.", NotificationManager.Type.Error);
                        }
                        break;
                }
                panel.Previous();
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void InitAAmenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 0, "JobTaxi", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ConfigEditor(player);
            });

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Taxi }, null, "Utiliser tablette taxi", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                LinePlayable.MainPanel(player);
            });

            _menu.AddAdminTabLine(PluginInformations, 5, $"{TextFormattingHelper.Color("CheckPoint Creator - JobTaxi", TextFormattingHelper.Colors.Grey)}", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                LineCreator.MainPanel(player);
            });
        }
    }
}
