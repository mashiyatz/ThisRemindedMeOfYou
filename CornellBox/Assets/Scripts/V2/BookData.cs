using System;
using System.Collections.Generic;

[Serializable]
public class BookEntry
{
    public string title;
    public string author;
    public string submittedAt;
    public string materialName;
    public string coverImagePath;
    public string responseSpriteResourcePath;
    public string responseSpriteImagePath;
    public string responseText;
    public string audioResourcePath;
    public string audioPath;
    public bool isHandwritten;
    public bool wantsNarrated;
    public string coverImageUrl;
}

[Serializable]
public class BookCatalog
{
    public List<BookEntry> books;
}
