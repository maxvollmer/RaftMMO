using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RaftMMO.Trade
{
    public class TradeItems
    {
        private static IEnumerable<Item_Base> GetAllWishableItems()
        {
            return ItemManager.GetAllItems().Where(IsWishableItem);
        }

        private static IEnumerable<Item_Base> GetAllOtherItems()
        {
            return GetAllWishableItems().Where(i => 
            (!IsPlaceableItem(i)
            && !IsNonPlaceableItem(i)
            && !IsCraftableResourceItem(i)
            && !IsFoodItem(i)
            && !IsSeed(i)
            && !IsFlowerOrColor(i)
            && !IsAnimalProduct(i)));
        }

        private static IEnumerable<Item_Base> GetPlaceableItems()
        {
            return GetAllWishableItems().Where(IsPlaceableItem).OrderByDescending(i => i.settings_recipe.CraftingCategory);
        }

        private static IEnumerable<Item_Base> GetNonPlaceableItems()
        {
            return GetAllWishableItems().Where(IsNonPlaceableItem).OrderByDescending(i => i.settings_recipe.CraftingCategory);
        }

        private static IEnumerable<Item_Base> GetCraftableResourceItems()
        {
            return GetAllWishableItems().Where(IsCraftableResourceItem).OrderBy(i => i.settings_recipe.SubCategoryOrder);
        }

        private static IEnumerable<Item_Base> GetFoodItems()
        {
            return GetAllWishableItems().Where(IsFoodItem).OrderBy(i => i.settings_consumeable, new FoodItemComparer());
        }

        private static IEnumerable<Item_Base> GetSeeds()
        {
            return GetAllWishableItems().Where(IsSeed);
        }

        private static IEnumerable<Item_Base> GetFlowersAndColors()
        {
            return GetAllWishableItems().Where(IsFlowerOrColor);
        }

        private static IEnumerable<Item_Base> GetAnimalProducts()
        {
            return GetAllWishableItems().Where(IsAnimalProduct);
        }

        private class FoodItemComparer : IComparer<ItemInstance_Consumeable>
        {
            public int Compare(ItemInstance_Consumeable x, ItemInstance_Consumeable y)
            {
                if (x.FoodType != y.FoodType)
                {
                    return x.FoodType == FoodType.Water ? -1 : 1;
                }

                if (x.BonusHungerYield != y.BonusHungerYield)
                {
                    return (x.BonusHungerYield < y.BonusHungerYield) ? -1 : 1;
                }

                if (x.HungerYield != y.HungerYield)
                {
                    return (x.HungerYield < y.HungerYield) ? -1 : 1;
                }

                if (x.ThirstYield != y.ThirstYield)
                {
                    return (x.ThirstYield < y.ThirstYield) ? -1 : 1;
                }

                return 0;
            }
        }

        private static bool IsFoodItem(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return item.settings_consumeable.FoodForm != FoodForm.None
                && item.settings_consumeable.FoodType != FoodType.None
                && item.settings_consumeable.FoodType != FoodType.SaltWater
                && !uniqueName.Equals("egg");
        }

        private static bool IsPlaceableItem(Item_Base item)
        {
            return IsCraftableItem(item) && item.settings_buildable.Placeable;
        }

        private static bool IsNonPlaceableItem(Item_Base item)
        {
            return IsCraftableItem(item) && !item.settings_buildable.Placeable;
        }

        private static bool IsCraftableItem(Item_Base item)
        {
            return item.settings_recipe.CraftingCategory != CraftingCategory.Nothing
                && item.settings_recipe.CraftingCategory != CraftingCategory.Hidden
                && item.settings_recipe.CraftingCategory != CraftingCategory.CreativeMode
                && item.settings_recipe.CraftingCategory != CraftingCategory.Resources
                && item.settings_recipe.CanCraft;
        }

        private static bool IsCraftableResourceItem(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return (item.settings_recipe.CraftingCategory == CraftingCategory.Resources && item.settings_recipe.CanCraft)
                || uniqueName.Contains("ingot")
                || uniqueName.Equals("brick_dry")
                || uniqueName.Equals("biofuel")
                || uniqueName.Equals("glass")
                || uniqueName.Equals("explosivepowder")
                || uniqueName.Equals("vinegoo");
        }

        private static bool IsSeed(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return uniqueName.Contains("seed") && !IsFlowerOrColor(item);
        }

        private static bool IsFlowerOrColor(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return uniqueName.Contains("flower")
                || uniqueName.Contains("color");
        }

        private static bool IsAnimalProduct(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return (uniqueName.StartsWith("head_"))
                || uniqueName.Equals("bucket_milk")
                || uniqueName.Equals("wool")
                || uniqueName.Equals("feather")
                || uniqueName.Equals("honeycomb")
                || uniqueName.Equals("leather")
                || uniqueName.Equals("placeable_giantclam")
                || uniqueName.Equals("jar_bee")
                || uniqueName.Equals("egg");
        }

        private static List<Item_Base> buildableItems = null;

        private static bool IsWishableItem(Item_Base item)
        {
            if (buildableItems == null)
            {
                buildableItems = ItemManager.GetBuildableItems();
            }

            var displayName = item.settings_Inventory.DisplayName.ToLower(CultureInfo.InvariantCulture);
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return (item.settings_recipe.CraftingCategory == CraftingCategory.Nothing || item.settings_recipe.CanCraft)
                && !displayName.Contains("an item")
                && !uniqueName.Contains("blueprint")
                && !uniqueName.Equals("beachball")
                && !uniqueName.Equals("repair")
                && !uniqueName.Equals("captainshat")
                && !uniqueName.Equals("pilothelmet")
                && !uniqueName.Equals("block_foundationarmor")
                && !uniqueName.Equals("devhat")
                && !uniqueName.Equals("mayorhat")
                && !uniqueName.Equals("tikimask")
                && !uniqueName.Contains("cassette")
                && item.settings_consumeable.FoodType != FoodType.SaltWater
                && (!buildableItems.Contains(item) || IsPlaceableItem(item))
                && (!IsAfterRadioTower(item) || AfterRadioTowerAllowed())
                && (!IsAfterVasagatan(item) || AfterVasagatanAllowed())
                && (!IsSmelterObject(item) || SmeltingAllowed())
                && (!IsCookedFood(item) || CookingAllowed())
                && (!IsColor(item) || ColorsAllowed())
                && (!IsBottleOfWater(item) || IsBottleOfWaterAllowed())
                && (!IsAfterCaravan(item) || IsIsAfterCaravanAllowed());
        }

        private static bool IsAfterCaravan(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);
            return uniqueName.Contains("banana")
                || uniqueName.Contains("strawberry")
                || uniqueName.Contains("titanium");
        }

        private static bool IsIsAfterCaravanAllowed()
        {
            if (GameModeValueManager.GetCurrentGameModeValue().gameMode == GameMode.Creative)
                return true;

            var localPlayer = ComponentManager<Raft_Network>.Value.GetLocalPlayer();

            return localPlayer.NoteBookUI.caravanNotes.Where(note => note.isUnlocked).Any()
                || localPlayer.NoteBookUI.tangaroaNotes.Where(note => note.isUnlocked).Any()
                || QuestProgressTracker.HasFinishedQuest(QuestType.TangaroaGeneratorRepaired)
                || QuestProgressTracker.HasFinishedQuest(QuestType.TangaroaFloodStarted)
                || ComponentManager<ChunkManager>.Value.currentSpawners.Contains(ComponentManager<ChunkManager>.Value.GetSpawner(ChunkPointType.Landmark_Tangaroa));
        }

        private static bool IsBottleOfWater(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);
            return uniqueName.Equals("plasticbottle_water");
        }

        private static bool IsBottleOfWaterAllowed()
        {
            if (GameModeValueManager.GetCurrentGameModeValue().gameMode == GameMode.Creative)
                return true;

            return ItemManager.GetAllItems()
                .Where(item => item.settings_recipe.CanCraft)
                .Select(item => item.UniqueName.ToLower(CultureInfo.InvariantCulture))
                .Where(uniqueName => uniqueName == "plasticbottle_empty")
                .Any();
        }


        private static bool IsColor(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);
            return uniqueName.Contains("color");
        }

        private static bool ColorsAllowed()
        {
            if (GameModeValueManager.GetCurrentGameModeValue().gameMode == GameMode.Creative)
                return true;

            return ItemManager.GetAllItems()
                .Where(item => item.settings_recipe.CanCraft)
                .Select(item => item.UniqueName.ToLower(CultureInfo.InvariantCulture))
                .Where(uniqueName => uniqueName == "placeable_paintmill")
                .Any();
        }

        private static bool IsCookedFood(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);
            return item.settings_consumeable.FoodType == FoodType.Food
                && (uniqueName.StartsWith("claybowl_") || uniqueName.StartsWith("clayplate_"))
                && !uniqueName.Equals("claybowl_empty");
        }

        private static bool CookingAllowed()
        {
            if (GameModeValueManager.GetCurrentGameModeValue().gameMode == GameMode.Creative)
                return true;

            return ItemManager.GetAllItems()
                .Where(item => item.settings_recipe.CanCraft)
                .Select(item => item.UniqueName.ToLower(CultureInfo.InvariantCulture))
                .Where(uniqueName => uniqueName == "placeable_cookingpot")
                .Any();
        }

        private static bool IsSmelterObject(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return uniqueName.Equals("explosivegoo")
                || uniqueName.Equals("vinegoo")
                || uniqueName.Equals("metalingot")
                || uniqueName.Equals("copperingot")
                || uniqueName.Equals("glass");
        }

        private static bool SmeltingAllowed()
        {
            if (GameModeValueManager.GetCurrentGameModeValue().gameMode == GameMode.Creative)
                return true;

            return ItemManager.GetAllItems()
                .Where(item => item.settings_recipe.CanCraft)
                .Select(item => item.UniqueName.ToLower(CultureInfo.InvariantCulture))
                .Where(uniqueName => uniqueName == "placeable_cookingstand_smelter")
                .Any();
        }

        private static bool IsAfterRadioTower(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return uniqueName.Equals("egg")
                || uniqueName.Equals("wool")
                || uniqueName.Equals("bucket_milk")
                || uniqueName.Equals("leather")
                || uniqueName.Equals("head_boar")
                || uniqueName.Equals("head_poisonpuffer")
                || uniqueName.Equals("head_screecher")
                || uniqueName.Equals("explosivepowder")
                || uniqueName.Equals("explosivegoo")
                || uniqueName.Equals("berries_red")
                || uniqueName.Equals("cavemushroom")
                || uniqueName.Equals("silveralgae")
                || IsAfterVasagatan(item);
        }

        private static bool AfterRadioTowerAllowed()
        {
            if (GameModeValueManager.GetCurrentGameModeValue().gameMode == GameMode.Creative)
                return true;

            return QuestProgressTracker.HasFinishedQuest(QuestType.RadioTowerReached);
        }

        private static bool IsAfterVasagatan(Item_Base item)
        {
            var uniqueName = item.UniqueName.ToLower(CultureInfo.InvariantCulture);

            return uniqueName.Equals("seed_birch")
                || uniqueName.Equals("seed_pine")
                || uniqueName.Equals("head_bear")
                || uniqueName.Equals("head_mamabear")
                || uniqueName.Equals("jar_bee")
                || uniqueName.Equals("honeycomb")
                || uniqueName.Equals("jar_honey")
                || uniqueName.Equals("biofuel")
                || uniqueName.Equals("machete");
        }

        private static bool AfterVasagatanAllowed()
        {
            if (GameModeValueManager.GetCurrentGameModeValue().gameMode == GameMode.Creative)
                return true;

            var localPlayer = ComponentManager<Raft_Network>.Value.GetLocalPlayer();
            return localPlayer.NoteBookUI.vasagatanNotes.Where(note => note.isUnlocked).Any()
                || localPlayer.NoteBookUI.balboaNotes.Where(note => note.isUnlocked).Any()
                || localPlayer.NoteBookUI.caravanNotes.Where(note => note.isUnlocked).Any()
                || localPlayer.NoteBookUI.tangaroaNotes.Where(note => note.isUnlocked).Any()
                || QuestProgressTracker.HasFinishedQuest(QuestType.MamaBearLurePlaced)
                || QuestProgressTracker.HasFinishedQuest(QuestType.MamaBearKilled)
                || QuestProgressTracker.HasFinishedQuest(QuestType.BalboaRelayStation1)
                || QuestProgressTracker.HasFinishedQuest(QuestType.BalboaRelayStation2)
                || QuestProgressTracker.HasFinishedQuest(QuestType.BalboaRelayStation3)
                || ComponentManager<ChunkManager>.Value.currentSpawners.Contains(ComponentManager<ChunkManager>.Value.GetSpawner(ChunkPointType.Landmark_Balboa));
        }

        public static IEnumerable<IEnumerable<Item_Base>> GetTradeItems()
        {
            yield return GetFoodItems();
            yield return GetPlaceableItems();
            yield return GetNonPlaceableItems();
            yield return GetSeeds();
            yield return GetFlowersAndColors();
            yield return GetAnimalProducts();
            yield return GetCraftableResourceItems();
            yield return GetAllOtherItems(); // resources found in ocean and on islands
        }
    }
}
