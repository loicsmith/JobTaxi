using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;
using Newtonsoft.Json;
using System.Linq;

namespace JobTaxi.Functions
{
    internal class CheckPointCreator
    {
        public ModKit.ModKit Context { get; set; }

        public async void MainPanel(Player player)
        {
            Panel panel = Context.PanelHelper.Create("CheckPoint Creator | Accueil", UIPanel.PanelType.TabPrice, player, () => MainPanel(player));

            var CheckpointData = await OrmManager.JobTaxi_JobTaxiManager.QueryAll();

            foreach (OrmManager.JobTaxi_JobTaxiManager Data in CheckpointData)
            {
                panel.AddTabLine("ID : " + Data.Id, "", ItemUtils.GetIconIdByItemId(1112), _ => { MoreOptions(player, Data); });
            }

            panel.NextButton("Ajouter", () => AddCheckpoint(player));
            panel.NextButton("Options", () => panel.SelectTab());

            panel.AddButton("Retour", ui => AAMenu.AAMenu.menu.AdminPanel(player));
            panel.CloseButton();
            panel.Display();
        }

        public void AddCheckpoint(Player player)
        {
            Panel panel = Context.PanelHelper.Create("CheckPoint Creator | Ajouter", UIPanel.PanelType.Text, player, () => AddCheckpoint(player));

            float PosX = player.setup.transform.position.x;
            float PosY = player.setup.transform.position.y;
            float PosZ = player.setup.transform.position.z;

            panel.TextLines.Add("Veuillez confirmer la position de l'arrêt de taxi");
            panel.TextLines.Add("Position X : " + PosX);
            panel.TextLines.Add("Position Y : " + PosY);
            panel.TextLines.Add("Position Z : " + PosZ);

            panel.AddButton("Confirmer", async (ui) =>
            {

                OrmManager.JobTaxi_JobTaxiManager instance = new OrmManager.JobTaxi_JobTaxiManager { PositionX = PosX, PositionY = PosY, PositionZ = PosZ };
                var result = await instance.Save();

                if (result)
                {
                    player.Notify("CheckPointCreator", $"Le checkpoint vient d'être crée", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("CheckPointCreator", $"Une erreur est survenue lors de la création du checkpoint", NotificationManager.Type.Error);
                }
                player.ClosePanel(ui);
                MainPanel(player);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void MoreOptions(Player player, OrmManager.JobTaxi_JobTaxiManager CheckPointManager)
        {
            Panel panel = Context.PanelHelper.Create("CheckPointCreator | More Options", UIPanel.PanelType.Text, player, () => MoreOptions(player, CheckPointManager));

            panel.TextLines.Add($"{TextFormattingHelper.Bold(TextFormattingHelper.Align("Informations :", TextFormattingHelper.Aligns.Center))}");
            panel.TextLines.Add("ID : " + CheckPointManager.Id);
            panel.TextLines.Add("Position X : " + CheckPointManager.PositionX);
            panel.TextLines.Add("Position Y : " + CheckPointManager.PositionY);
            panel.TextLines.Add("Position Z : " + CheckPointManager.PositionZ);

            panel.AddButton("S'y Téléporter", (ui) =>
            {
                player.setup.TargetSetPosition(new UnityEngine.Vector3(CheckPointManager.PositionX, CheckPointManager.PositionY, CheckPointManager.PositionZ));
                player.Notify("CheckPointCreator", $"Téléportation vers le checkpoint !", NotificationManager.Type.Success);
            });

            panel.AddButton("Supprimer", (ui) =>
            {
                player.ClosePanel(ui);
                Delete(player, CheckPointManager);
            });

            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

        public void Delete(Player player, OrmManager.JobTaxi_JobTaxiManager CheckPointManager)
        {
            Panel panel = Context.PanelHelper.Create("LineCreator | Delete Line", UIPanel.PanelType.Text, player, () => Delete(player, CheckPointManager));
            panel.TextLines.Add($"Veuillez confirmer la supression du checkpoint");

            panel.AddButton("Valider", async (ui) =>
            {
                var result = await CheckPointManager.Delete();

                if (result)
                {
                    player.Notify("CheckPointCreator", $"Le checkpoint vient d'être supprimé", NotificationManager.Type.Success);
                }
                else
                {
                    player.Notify("CheckPointCreator", $"Une erreur est survenue lors de la suppression du checkpoint", NotificationManager.Type.Error);
                }
                player.ClosePanel(ui);
                MainPanel(player);
            });
            panel.PreviousButton();
            panel.CloseButton();
            panel.Display();
        }

    }
}