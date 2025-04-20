using Life;
using Life.CheckpointSystem;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace JobTaxi.Functions
{
    internal class CheckPointPlayable
    {
        public List<uint> PlayerNetID = new List<uint>();

        public ModKit.ModKit Context { get; set; }

        private Dictionary<int, Coroutine> activeCoroutines = new Dictionary<int, Coroutine>();

        public void MainPanel(Player player)
        {

            Panel panel = Context.PanelHelper.Create("Tablette Taxi | Accueil", UIPanel.PanelType.Text, player, () => MainPanel(player));

            panel.TextLines.Add("Bienvenue.\n Souhaitez-vous recevoir des courses ?");
            panel.AddButton("C'est partie !", (ui) => { ServiceManager(player); player.ClosePanel(panel); });

            panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.BizPanel(player));
            panel.CloseButton();
            panel.Display();


        }


        public void ServiceManager(Player player)
        {
            if (!PlayerNetID.Contains(player.setup.netId) && !activeCoroutines.ContainsKey(player.character.Id))
            {
                Coroutine coroutine = Nova.man.StartCoroutine(CheckCall(player));
                activeCoroutines[player.character.Id] = coroutine;
            }
            else
            {
                ThrowPassenger(player);
            }
        }

        public IEnumerator CheckCall(Player player)
        {
            while (true)
            {
                if (!PlayerNetID.Contains(player.setup.netId))
                {
                    if (UnityEngine.Random.Range(0, 100) > 50)
                    {
                        StartTaxiService(player);
                    }
                }
                yield return new WaitForSeconds(60f);
            }
        }

        public void ThrowPassenger(Player player)
        {
            Panel panel = Context.PanelHelper.Create($"Tablette Taxi - Informations", UIPanel.PanelType.Text, player, () => ThrowPassenger(player));
            panel.TextLines.Add($"Vous effectuez déja une course ou êtes en service ! Souhaitez vous déposer les passagers sur le trottoir ou retirer votre service ?");

            panel.AddButton("Oui", (ui) =>
            {
                player.ClosePanel(ui);
                PlayerNetID.Remove(player.setup.netId);
                player.DestroyAllVehicleCheckpoint();
                player.setup.TargetDisableNavigation();

                if (activeCoroutines.ContainsKey(player.character.Id))
                {
                    Nova.man.StopCoroutine(activeCoroutines[player.character.Id]);
                    activeCoroutines.Remove(player.character.Id);
                }
            });

            panel.AddButton("Non", (ui) =>
            {
                player.ClosePanel(ui);
            });

            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void RemoveNetIDFromList(int ConnID)
        {
            PlayerNetID.Remove((uint)ConnID);
        }

        public async void StartTaxiService(Player player)
        {
            var Data = await OrmManager.JobTaxi_JobTaxiManager.QueryAll();

            int startIndex, endIndex;
            do
            {
                startIndex = UnityEngine.Random.Range(0, Data.Count);
                endIndex = UnityEngine.Random.Range(0, Data.Count);
            } while (startIndex == endIndex);

            var startStop = Data[startIndex];
            var endStop = Data[endIndex];

            float startX = startStop.PositionX;
            float startY = startStop.PositionY;
            float startE = startStop.PositionZ;

            float endX = endStop.PositionX;
            float endY = endStop.PositionY;
            float endE = endStop.PositionZ;

            NVehicleCheckpoint[] checkpoints = new NVehicleCheckpoint[2];

            checkpoints[0] = new NVehicleCheckpoint(player.netId, new Vector3(startX, startY, startE), async (c, vId) =>
            {
                player.Notify("Taxi", $"Allez chercher votre client !", NotificationManager.Type.Info);

                player.DestroyVehicleCheckpoint(c);
                player.setup.TargetDisableNavigation();

                player.Notify("Taxi", "Votre client monte dans le taxi..", NotificationManager.Type.Info, 5f);
                player.IsFreezed = true;

                await Task.Delay(5000);

                player.IsFreezed = false;

                player.CreateVehicleCheckpoint(checkpoints[1]);
                player.setup.TargetSetGPSTarget(new Vector3(endX, endY, endE));

                player.Notify("Taxi", $"Allez à l'adresse émise par le client", NotificationManager.Type.Info);
            });

            checkpoints[1] = new NVehicleCheckpoint(player.netId, new Vector3(endX, endY, endE), async (c, vId) =>
            {
                player.Notify("Taxi", $"Vous avez atteint votre destination !", NotificationManager.Type.Success);

                player.DestroyVehicleCheckpoint(c);
                player.setup.TargetDisableNavigation();

                player.Notify("Taxi", "Les passagers descendent du taxi..", NotificationManager.Type.Info, 5f);
                player.IsFreezed = true;

                await Task.Delay(5000);

                ReceiveMoney(player);
                player.IsFreezed = false;

                player.Notify("Taxi", "Le trajet est terminé. Merci pour votre service !", NotificationManager.Type.Success);

                PlayerNetID.Remove(player.setup.netId);
            });

            player.CreateVehicleCheckpoint(checkpoints[0]);
            player.setup.TargetSetGPSTarget(new Vector3(startX, startY, startE));

            PlayerNetID.Add(player.setup.netId);

            player.Notify("Taxi", $"Un client vient de vous appeller, rejoigner le ! Bon trajet !", NotificationManager.Type.Success, 10f);
        }

        public void ReceiveMoney(Player player)
        {
            float totalMoney = 0f;


            float money = UnityEngine.Random.Range(Main.Main._JobTaxiConfig.MinMoneyPerCustomer, Main.Main._JobTaxiConfig.MaxMoneyPerCustomer + 1);
            totalMoney += money;


            float taxPercentage = Main.Main._JobTaxiConfig.TaxPercentage;
            float PlayerReceivePercentage = Main.Main._JobTaxiConfig.PlayerReceivePercentage;

            float cityHallMoney = totalMoney * (taxPercentage / 100f);

            float playerMoney = totalMoney * (PlayerReceivePercentage / 100f); ;

            float BusMoney = totalMoney - cityHallMoney - playerMoney;

            if (Nova.biz.FetchBiz(Main.Main._JobTaxiConfig.CityHallId) != null)
            {
                Nova.biz.FetchBiz(Main.Main._JobTaxiConfig.CityHallId).Bank += Math.Round(cityHallMoney, 2);
                Nova.biz.FetchBiz(Main.Main._JobTaxiConfig.CityHallId).Save();
            }

            player.biz.Bank += Math.Round(BusMoney, 2);
            player.biz.Save();

            player.AddBankMoney(Math.Round(playerMoney, 2));
            player.character.Save();

            player.Notify("Gains", $"Vous venez de reçevoir {TextFormattingHelper.Color(Math.Round(playerMoney, 2).ToString(), TextFormattingHelper.Colors.Orange)}€", NotificationManager.Type.Info);
        }
    }
}