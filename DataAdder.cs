using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Xml;
using System.Xml.Serialization;
using HarmonyLib;
using LOR_DiceSystem;
using LOR_XML;
using Mod;
using Sound;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Workshop;
using static DataAdder.AddDataUtil;
using static DataAdder.Localizer;
using static DataAdder.ModInfo;
using FileInfo = System.IO.FileInfo;
using Random = UnityEngine.Random;

namespace DataAdder
{
	public class DataAdder : ModInitializer

	{
		public override void OnInitializeMod()
		{
			AppPath.GetPackageId(ref PackageID);
			try { GetArtWorks(ref ArtWorks); }
			catch (Exception ex) { ex.Error(AppPath); };
			//if (!Harmony.HasAnyPatches(PackageID)) { "SaveSelectionData".PrePatch<ModContentManager, GitTest>(); }
			//else { /*Patch();*/ }
			PackageID.AddNewData();
			PackageID.AddLocalize();
		}
		public static void InitBattleDialogByDefaultBook(UnitDataModel __instance, LorId lorId)
		{
			if (lorId.packageId != PackageID
				|| Singleton<BattleDialogXmlList>.Instance.GetCharacterData(PackageID, lorId.id.ToString()) == null) { return; }
			switch (lorId.id)
			{
				default:
					__instance.battleDialogModel = new BattleDialogueModel(Singleton<BattleDialogXmlList>.Instance.GetCharacterData(PackageID, lorId.id.ToString()));
					break;
			}
		}
	}
	public static class ModInfo
	{
		public static string AppPath => Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
		public static string Language => GlobalGameManager.Instance.CurrentOption.language;
		public static string PackageID;
		public static string TargetPath => Singleton<ModContentManager>.Instance.GetModPath(PackageID);
		public static Harmony H => new Harmony(PackageID);
		public static MethodInfo method;
		public static FileInfo[] files;
		public static string filename;
		public static Dictionary<string, Sprite> ArtWorks = new Dictionary<string, Sprite>();
		public static void GetPackageId(this string dir, ref string PackageID)
		{
			foreach (FileInfo file in new DirectoryInfo(dir).Parent.GetFiles())
			{
				using (StringReader stringReader = new StringReader(File.ReadAllText(file.FullName)))
				{
					NormalInvitation Invitation = (NormalInvitation)new XmlSerializer(typeof(NormalInvitation)).Deserialize(stringReader);
					PackageID = Invitation.workshopInfo.uniqueId;
				}
			}
		}
		public static void GetArtWorks(ref Dictionary<string, Sprite> ArtWorks)
		{
			if (!Directory.Exists(Path.Combine(TargetPath, "ArtWork"))) { return; }
			new DirectoryInfo(Path.Combine(TargetPath, "ArtWork")).GetArtWork(ref ArtWorks);
		}
		private static void GetArtWork(this DirectoryInfo dir, ref Dictionary<string, Sprite> ArtWorks)
		{
			if (dir.GetDirectories().Length != 0)
			{ foreach (var d in dir.GetDirectories()) { GetArtWork(d, ref ArtWorks); } }
			foreach (FileInfo fileInfo in dir.GetFiles())
			{
				Texture2D texture2D = new Texture2D(2, 2);
				texture2D.LoadImage(File.ReadAllBytes(fileInfo.FullName));
				ArtWorks[Path.GetFileNameWithoutExtension(fileInfo.FullName)] = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
			}
		}
		public static bool Error(this Exception ex, string AppPath)
		{
			FileLog.Log("=======================================");
			FileLog.Log(ex.Message + Environment.NewLine + ex.StackTrace.ToString());
			FileLog.Log("=======================================");
			using (StreamWriter streamWriter = File.AppendText(AppPath + "/errorLog_Wist107Util.txt"))
			{
				TextWriter textWriter = streamWriter;
				Exception ex2 = ex;
				textWriter.WriteLine((ex2?.ToString()) + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
			}
			return true;
		}
		public static MethodInfo PrePatch<T1, T2>(this string TargetMethod)
			=> H.Patch(typeof(T1).GetMethod(TargetMethod, AccessTools.all), new HarmonyMethod(typeof(T2).GetMethod(TargetMethod)));
		public static MethodInfo PostPatch<T1, T2>(this string TargetMethod)
			=> H.Patch(typeof(T1).GetMethod(TargetMethod, AccessTools.all), null, new HarmonyMethod(typeof(T2).GetMethod(TargetMethod)));
		public static void SetValue<T>(this T x, string target, object value)
			=> x.GetType().GetField(target, AccessTools.all).SetValue(x, value);
		public static T GetValue<T>(this object target, string name)
			=> (T)target.GetType().GetField(name, AccessTools.all).GetValue(target);
	}
	public static class Localizer
	{
		public static void AddLocalize(this string PackageID)
		{
			string FilePath = Path.Combine(TargetPath, "Localize", Language);
			if (!Directory.Exists(FilePath)) { return; }
			FilePath.AddBook(PackageID);
			FilePath.AddBattleCard(PackageID);
			FilePath.AddBattleCardAbilities();
			FilePath.AddBattleDialogues();
			try { "InitBattleDialogByDefaultBook".PrePatch<UnitDataModel, DataAdder>(); }
			catch (Exception ex) { ex.Error(AppPath); }
			FilePath.AddCharactersName(PackageID);
			FilePath.AddDropBook(PackageID);
			FilePath.AddEffectTexts();
			FilePath.AddEtc();
			FilePath.AddPassiveDesc(PackageID);
			FilePath.AddStageName(PackageID);
		}
		static void AddBattleCard(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "BattlesCards");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				StringReader SR = new StringReader(File.ReadAllText(file.FullName));
				if (SR == null) { continue; }
				BattleCardDescRoot Desc = (BattleCardDescRoot)new XmlSerializer(typeof(BattleCardDescRoot)).Deserialize(SR);
				if (Desc == null) { continue; }
				foreach (DiceCardXmlInfo card in ItemXmlDataList.instance.GetAllWorkshopData()[PackageID])
				{
					try
					{
						if (card == null || card.workshopName != string.Empty) { continue; }

						card.workshopName = Desc.cardDescList.Find((BattleCardDesc x) => x.cardID == card.id.id).cardName;
					}
					catch { }
				}
				typeof(ItemXmlDataList).GetField("_cardInfoTable", AccessTools.all).GetValue(ItemXmlDataList.instance);
				foreach (DiceCardXmlInfo card in ItemXmlDataList.instance.GetCardList().FindAll((DiceCardXmlInfo x) => x.id.packageId == PackageID))
				{
					try
					{
						if (card == null || card.workshopName != string.Empty) { continue; }
						card.workshopName = Desc.cardDescList.Find((BattleCardDesc x) => x.cardID == card.id.id).cardName;
						if (card.workshopName == null) { continue; }
						ItemXmlDataList.instance.GetCardItem(card.id).workshopName = card.workshopName;
					}
					catch { }
				}
			}
		}
		static void AddBattleCardAbilities(this string path)
		{
			string FilePath = Path.Combine(path, "BattleCardAbilities");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				try
				{
					using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
					{
						foreach (BattleCardAbilityDesc Desc in ((BattleCardAbilityDescRoot)new XmlSerializer(typeof(BattleCardAbilityDescRoot)).Deserialize(SR)).cardDescList)
						{
							try { Singleton<BattleCardAbilityDescXmlList>.Instance.GetData(Desc.id).desc = Desc.desc; }
							catch { }
						}
					}
				}
				catch { }
			}
		}
		static void AddBattleDialogues(this string path)
		{
			string FilePath = Path.Combine(path, "BattleDialogues");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				try
				{
					using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
					{
						BattleDialogRoot BDR = (BattleDialogRoot)new XmlSerializer(typeof(BattleDialogRoot)).Deserialize(SR);
						(
							(Dictionary<string, BattleDialogRoot>)typeof(BattleDialogXmlList)
							.GetField("_dictionary", AccessTools.all)
							.GetValue(Singleton<BattleDialogXmlList>.Instance)
							)
							[BDR.groupName] = BDR;
					}
				}
				catch { }
			}
		}
		static void AddBook(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "Books");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				string filename = file.FullName;
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					BookDescRoot BDR = (BookDescRoot)new XmlSerializer(typeof(BookDescRoot)).Deserialize(SR);
					foreach (BookXmlInfo bookXml in Singleton<BookXmlList>.Instance.GetAllWorkshopData()[PackageID])
					{
						try { bookXml.InnerName = BDR.bookDescList.Find((BookDesc x) => x.bookID == bookXml.id.id).bookName; }
						catch { }
					}
					foreach (BookXmlInfo bookXml in Singleton<BookXmlList>.Instance.GetList().FindAll((BookXmlInfo x) => x.id.packageId == PackageID))
					{
						try
						{
							bookXml.InnerName = BDR.bookDescList.Find((BookDesc x) => x.bookID == bookXml.id.id).bookName;
							Singleton<BookXmlList>.Instance.GetData(bookXml.id, true).InnerName = bookXml.InnerName;
						}
						catch { }
					}
					(typeof(BookDescXmlList).GetField("_dictionaryWorkshop", AccessTools.all).GetValue(Singleton<BookDescXmlList>.Instance) as Dictionary<string, List<BookDesc>>)[PackageID] = BDR.bookDescList;
				}
			}
		}
		static void AddCharactersName(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "CharactersName");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				string filename = file.FullName;
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					CharactersNameRoot CNR = (CharactersNameRoot)new XmlSerializer(typeof(CharactersNameRoot)).Deserialize(SR);
					foreach (EnemyUnitClassInfo enemy in Singleton<EnemyUnitClassInfoList>.Instance.GetAllWorkshopData()[PackageID])
					{
						try
						{
							enemy.name = CNR.nameList.Find((CharacterName x) => x.ID == enemy.id.id).name;
							Singleton<EnemyUnitClassInfoList>.Instance.GetData(enemy.id).name = enemy.name;
						}
						catch { }
					}
				}
			}
		}
		static void AddDropBook(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "DropBooks");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				string filename = file.FullName;
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					CharactersNameRoot CNR = (CharactersNameRoot)new XmlSerializer(typeof(CharactersNameRoot)).Deserialize(SR);
					foreach (DropBookXmlInfo dropBook in Singleton<DropBookXmlList>.Instance.GetAllWorkshopData()[PackageID])
					{
						try { dropBook.workshopName = CNR.nameList.Find((CharacterName x) => x.ID == dropBook.id.id).name; }
						catch { }
					}
					foreach (DropBookXmlInfo dropBook in Singleton<DropBookXmlList>.Instance.GetList().FindAll((DropBookXmlInfo x) => x.id.packageId == PackageID))
					{
						try
						{
							dropBook.workshopName = CNR.nameList.Find((CharacterName x) => x.ID == dropBook.id.id).name;
							Singleton<DropBookXmlList>.Instance.GetData(dropBook.id, false).workshopName = dropBook.workshopName;
						}
						catch { }
					}
				}
			}
		}
		static void AddEffectTexts(this string path)
		{
			string FilePath = Path.Combine(path, "EffectTexts");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			Dictionary<string, BattleEffectText> dic
				= typeof(BattleEffectTextsXmlList).GetField("_dictionary", AccessTools.all).GetValue(Singleton<BattleEffectTextsXmlList>.Instance) as Dictionary<string, BattleEffectText>;
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					var BETR = (BattleEffectTextRoot)new XmlSerializer(typeof(BattleEffectTextRoot)).Deserialize(SR);
					foreach (var Text in BETR.effectTextList)
					{
						try
						{
							if (dic.ContainsKey(Text.ID)) { dic.Remove(Text.ID); }
							dic.Add(Text.ID, Text);
						}
						catch { }
					}
				}
			}
		}
		static void AddEtc(this string path)
		{
			string FilePath = Path.Combine(path, "etc");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo fileInfo in new DirectoryInfo(FilePath).GetFiles())
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(File.ReadAllText(fileInfo.FullName));
				try
				{
					foreach (object obj in xmlDocument.SelectNodes("localize/text"))
					{
						try
						{
							XmlNode xmlNode = (XmlNode)obj;
							string key = string.Empty;
							if (xmlNode.Attributes.GetNamedItem("id") != null) { key = xmlNode.Attributes.GetNamedItem("id").InnerText; }
							if (TextDataModel.textDic.ContainsKey(key)) { continue; }
							TextDataModel.textDic.Add(key, xmlNode.InnerText);
						}
						catch { }
					}
				}
				catch { }
			}
		}
		static void AddPassiveDesc(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "PassiveDesc");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (var file in new DirectoryInfo(FilePath).GetFiles())
			{
				string filename = file.FullName;
				try
				{
					using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
					{
						PassiveDescRoot PDR = (PassiveDescRoot)new XmlSerializer(typeof(PassiveDescRoot)).Deserialize(SR);
						if (PDR == null) { continue; }
						foreach (PassiveXmlInfo passive in Singleton<PassiveXmlList>.Instance.GetDataAll().FindAll((PassiveXmlInfo x) => x.id.packageId == PackageID))
						{
							if (PDR.descList.Find((PassiveDesc x) => x.ID == passive.id.id) == null) { continue; }
							passive.name = PDR.descList.Find((PassiveDesc x) => x.ID == passive.id.id).name;
							passive.desc = PDR.descList.Find((PassiveDesc x) => x.ID == passive.id.id).desc;
						}
					}
				}
				catch { }
			}
		}
		static void AddStageName(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "StageName");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				string filename = file.FullName;
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					CharactersNameRoot CNR = (CharactersNameRoot)new XmlSerializer(typeof(CharactersNameRoot)).Deserialize(SR);
					foreach (StageClassInfo stage in Singleton<StageClassInfoList>.Instance.GetAllWorkshopData()[PackageID])
					{
						try { stage.stageName = CNR.nameList.Find((CharacterName x) => x.ID == stage.id.id).name; }
						catch { }
					}
				}
			}
		}
	}
	public static class AddDataUtil
	{
		public static void AddNewData(this string PackageID)
		{
			string FilePath = Path.Combine(TargetPath, "Data");
			if (!Directory.Exists(FilePath)) { return; }
			FilePath.AddFormationInfo();
			FilePath.AddEmotionCard();
			if (PackageID == "") { return; }
			FilePath.AddBattleCard(PackageID);
			FilePath.AddPassive(PackageID);
			FilePath.AddEquip(PackageID);
			FilePath.AddDeck(PackageID);
			FilePath.AddEnemy(PackageID);
			FilePath.AddStage(PackageID);
			FilePath.AddBook(PackageID);
		}
		static void AddFormationInfo(this string path)
		{
			string FilePath = Path.Combine(path, "FormationInfo");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			List<FormationXmlInfo> List = typeof(FormationXmlList).GetField("_list", AccessTools.all).GetValue(Singleton<FormationXmlList>.Instance) as List<FormationXmlInfo>;
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader stringReader = new StringReader(File.ReadAllText(file.FullName)))
				{
					try { List.AddRange(((FormationXmlRoot)new XmlSerializer(typeof(FormationXmlRoot)).Deserialize(stringReader)).list); }
					catch { }
				}
			}
		}
		static void AddEmotionCard(this string path)
		{
			string FilePath = Path.Combine(path, "EmotionCard");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			List<EmotionCardXmlInfo> List = typeof(EmotionCardXmlList).GetField("_list", AccessTools.all).GetValue(Singleton<EmotionCardXmlList>.Instance) as List<EmotionCardXmlInfo>;
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					try { List.AddRange(((EmotionCardXmlRoot)new XmlSerializer(typeof(EmotionCardXmlRoot)).Deserialize(SR)).emotionCardXmlList); }
					catch { }
				}
			}
		}
		static void AddBattleCard(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "CardInfo");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					DiceCardXmlRoot cardList = (DiceCardXmlRoot)new XmlSerializer(typeof(DiceCardXmlRoot)).Deserialize(SR);
					foreach (DiceCardXmlInfo XmlInfo in cardList.cardXmlList)
					{ XmlInfo.workshopID = PackageID; }
					try
					{ ItemXmlDataList.instance.AddCardInfoByMod(PackageID, cardList.cardXmlList); }
					catch (Exception ex) { ex.Error(AppPath); };
				}
			}
		}
		static void AddPassive(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "PassiveList");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					PassiveXmlRoot List = (PassiveXmlRoot)new XmlSerializer(typeof(PassiveXmlRoot)).Deserialize(SR);
					foreach (PassiveXmlInfo XmlInfo in List.list)
					{ XmlInfo.workshopID = PackageID; }
					try
					{ Singleton<PassiveXmlList>.Instance.AddPassivesByMod(List.list); }
					catch (Exception ex) { ex.Error(AppPath); };
				}
			}
		}
		static void AddEquip(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "EquipPage");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					BookXmlRoot List = (BookXmlRoot)new XmlSerializer(typeof(BookXmlRoot)).Deserialize(SR);
					foreach (BookXmlInfo XmlInfo in List.bookXmlList)
					{
						XmlInfo.workshopID = PackageID;
						LorId.InitializeLorIds(XmlInfo.EquipEffect._PassiveList, XmlInfo.EquipEffect.PassiveList, PackageID);
						if (string.IsNullOrEmpty(XmlInfo.skinType)) { continue; }
						switch (XmlInfo.skinType)
						{
							case "UNKNOWN": XmlInfo.skinType = "Lor"; break;
							case "CUSTOM": XmlInfo.skinType = "Custom"; break;
							case "LOR": XmlInfo.skinType = "Lor"; break;
						}
					}
					try { Singleton<BookXmlList>.Instance.AddEquipPageByMod(PackageID, List.bookXmlList); }
					catch (Exception ex) { ex.Error(AppPath); };
				}
			}
		}
		static void AddDeck(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "Deck");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					DeckXmlRoot List = (DeckXmlRoot)new XmlSerializer(typeof(DeckXmlRoot)).Deserialize(SR);
					foreach (DeckXmlInfo XmlInfo in List.deckXmlList)
					{
						XmlInfo.workshopId = PackageID;
						XmlInfo.cardIdList.Clear();
						LorId.InitializeLorIds(XmlInfo._cardIdList, XmlInfo.cardIdList, PackageID);
					}
					try { Singleton<DeckXmlList>.Instance.AddDeckByMod(List.deckXmlList); }
					catch (Exception ex) { ex.Error(AppPath); };
				}
			}
		}
		static void AddEnemy(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "Enemy");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			List<EnemyUnitClassInfo> list = new List<EnemyUnitClassInfo>();
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					EnemyUnitClassRoot List = (EnemyUnitClassRoot)new XmlSerializer(typeof(EnemyUnitClassRoot)).Deserialize(SR);
					foreach (EnemyUnitClassInfo XmlInfo in List.list)
					{
						XmlInfo.workshopID = PackageID;
						XmlInfo.height = RandomUtil.Range(XmlInfo.minHeight, XmlInfo.maxHeight);
					}
					try { list.AddRange(List.list); }
					catch (Exception ex) { ex.Error(AppPath); };
				}
			}
			Singleton<EnemyUnitClassInfoList>.Instance.AddEnemyUnitByMod(PackageID, list);
			Singleton<EnemyUnitClassInfoList>.Instance.GetAllWorkshopData()[PackageID].AddRange(list);
		}
		static void AddStage(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "Stage");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			List<StageClassInfo> list = new List<StageClassInfo>();
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					StageXmlRoot List = (StageXmlRoot)new XmlSerializer(typeof(StageXmlRoot)).Deserialize(SR);
					foreach (StageClassInfo XmlInfo in List.list)
					{
						XmlInfo.workshopID = PackageID;
						XmlInfo.InitializeIds(PackageID);
						foreach (StageStoryInfo stageStoryInfo in XmlInfo.storyList)
						{
							stageStoryInfo.packageId = PackageID;
							stageStoryInfo.valid = true;
						}
						if (XmlInfo.invitationInfo.combine == StageCombineType.BookRecipe)
						{
							XmlInfo.invitationInfo.needsBooks.Sort();
							XmlInfo.invitationInfo.needsBooks.Reverse();
							Singleton<StageClassInfoList>.Instance.GetValue<List<StageClassInfo>>("_workshopRecipeList").Add(XmlInfo);
						}
						else if (XmlInfo.invitationInfo.combine == StageCombineType.BookValue)
						{
							int bookNum = XmlInfo.invitationInfo.bookNum;
							if (bookNum >= 1 && bookNum <= 3)
							{
								Dictionary<int, List<StageClassInfo>> dict = Singleton<StageClassInfoList>.Instance.GetValue<Dictionary<int, List<StageClassInfo>>>("_workshopValueDict");
								dict[bookNum].Add(XmlInfo);
								Comparison<StageClassInfo> comparison = (StageClassInfo info1, StageClassInfo info2) => (int)(10f * (info2.invitationInfo.bookValue - info1.invitationInfo.bookValue));
								dict[1].Sort(comparison);
								dict[2].Sort(comparison);
								dict[3].Sort(comparison);
							}
						}
					}
					try { list.AddRange(List.list); }
					catch (Exception ex) { ex.Error(AppPath); };
				}
			}
			Singleton<StageClassInfoList>.Instance.AddStageByMod(PackageID, list);
			Singleton<StageClassInfoList>.Instance.GetAllWorkshopData()[PackageID].AddRange(list);
		}
		static void AddBook(this string path, string PackageID)
		{
			string FilePath = Path.Combine(path, "DropBook");
			if (!Directory.Exists(FilePath) || new DirectoryInfo(FilePath).GetFiles() == null) { return; }
			List<DropBookXmlInfo> list = new List<DropBookXmlInfo>();
			foreach (FileInfo file in new DirectoryInfo(FilePath).GetFiles())
			{
				if (file == null) { continue; }
				using (StringReader SR = new StringReader(File.ReadAllText(file.FullName)))
				{
					BookUseXmlRoot List = (BookUseXmlRoot)new XmlSerializer(typeof(BookUseXmlRoot)).Deserialize(SR);
					foreach (DropBookXmlInfo XmlInfo in List.bookXmlList)
					{
						XmlInfo.workshopID = PackageID;
						XmlInfo.InitializeDropItemList(PackageID);
					}
					try { list.AddRange(List.bookXmlList); }
					catch (Exception ex) { ex.Error(AppPath); };
				}
			}
			Singleton<DropBookXmlList>.Instance.SetDropTableByMod(list);
			Singleton<DropBookXmlList>.Instance.AddBookByMod(PackageID, list);

		}
	}
}