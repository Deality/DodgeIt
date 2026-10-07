using System.Collections.Generic;
using System.Text;

// Takma adlar için basit küfür / hakaret filtresi. Liderlik tablosunda herkes birbirinin adını gördüğü için
// hem ad seçilirken (NicknameManager) hem de tablo gösterilirken (LeaderboardManager) kullanılır.
// Her şeyi yakalamaz; amaç en yaygın ve en ağır kelimeleri engellemek.
public static class NameFilter
{
    // Adın herhangi bir yerinde geçmesi yeterli olan kelimeler (harfler sadeleştirildikten sonra aranır)
    private static readonly string[] BlockedAnywhere =
    {
        // English
        "fuck", "shit", "bitch", "cunt", "dick", "cock", "pussy", "nigger", "nigga", "faggot", "whore", "slut",
        "rapist", "hitler", "porn", "penis", "vagina", "asshole", "bastard", "retard", "blowjob", "handjob",
        "dildo", "boob", "horny", "pedofil", "pedophil", "molest",
        // Türkçe (ı,ş,ç,ğ,ö,ü sadeleştirilmiş hâlleriyle)
        "amk", "aminak", "aminas", "amcik", "amcuk", "siker", "sikis", "sikik", "sikim", "sikeyim", "siktir",
        "sokayim", "sokarim", "yarrak", "yarak", "orospu", "orosbu", "ibne", "pezevenk", "kahpe", "kaltak",
        "gavat", "yavsak", "serefsiz", "gotveren", "gotos", "gotlek", "tasak", "dassak", "surtuk", "fahise",
        "anani", "ananin", "bacini", "avradini"
    };

    // Kısa oldukları ya da masum kelimelerin içinde de geçtikleri için ("klasik" içindeki "sik", "Nazım"
    // içindeki "nazi", "grape" içindeki "rape" gibi) yalnızca adın tamamıysa ya da rakam / işaretle ayrılmış
    // bir parçasıysa engellenen kelimeler.
    private static readonly string[] BlockedWholeToken =
    {
        "sex", "sik", "aq", "amq", "oc", "pic", "got", "fag", "ass", "cum", "xxx", "am", "amina", "amini",
        "rape", "anal", "nazi", "kkk", "tits", "nude", "pedo", "wank", "pust"
    };

    private static readonly HashSet<string> WholeTokens = new HashSet<string>(BlockedWholeToken);

    public static bool IsAllowed(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;

        string letters = Simplify(name, keepSeparators: false);
        string collapsed = CollapseRuns(letters);
        foreach (string word in BlockedAnywhere)
        {
            if (letters.Contains(word)) return false;
            // "fuuuck" gibi uzatmalar: tekrar eden harfler teke indirilip öyle de bakılır
            string w = CollapseRuns(word);
            if (w != "niger" && collapsed.Contains(w)) return false; // "niger" masum adlarda da geçer
        }

        foreach (string token in Simplify(name, keepSeparators: true).Split(' '))
        {
            if (token.Length == 0) continue;
            if (WholeTokens.Contains(token) || WholeTokens.Contains(CollapseRuns(token))) return false;
        }
        return true;
    }

    // Küçük harfe çevirir, Türkçe harfleri ve yaygın harf yerine rakam / işaret kullanımını sadeleştirir.
    // keepSeparators: harf olmayan karakterler boşluğa çevrilir (parça parça bakmak için); değilse atılır.
    private static string Simplify(string s, bool keepSeparators)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char raw in s)
        {
            char c = char.ToLowerInvariant(raw);
            switch (c)
            {
                case 'ı': case 'i': case 'İ': case '1': case '!': c = 'i'; break;
                case 'ş': case '$': case '5': c = 's'; break;
                case 'ç': c = 'c'; break;
                case 'ğ': c = 'g'; break;
                case 'ö': case '0': c = 'o'; break;
                case 'ü': c = 'u'; break;
                case '@': case '4': c = 'a'; break;
                case '3': c = 'e'; break;
                case '7': c = 't'; break;
            }
            // 'İ' küçültülünce iki karaktere dönüşebilir; noktalı işareti at
            if (c == '̇') continue;

            if (c >= 'a' && c <= 'z') sb.Append(c);
            else if (keepSeparators) sb.Append(' ');
        }
        return sb.ToString();
    }

    private static string CollapseRuns(string s)
    {
        var sb = new StringBuilder(s.Length);
        char prev = '\0';
        foreach (char c in s)
        {
            if (c != prev) sb.Append(c);
            prev = c;
        }
        return sb.ToString();
    }
}
