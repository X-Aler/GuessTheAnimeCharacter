using System;
using System.Collections.Generic;

[Serializable]
public class CharacterData
{
    public int id;
    public string name_ru;
    public string name_en;
    public string card_asset_name;
    public string quote_ru;
    public string quote_en;
    public string GetName(string lang) => lang == "ru" ? name_ru : name_en;
    public string GetQuote(string lang) => lang == "ru" ? quote_ru : quote_en;
}

[Serializable]
public class CharacterDatabase
{
    public List<CharacterData> characters;
}