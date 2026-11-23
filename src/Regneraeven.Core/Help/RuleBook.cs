using Regneraeven.Core.Topics;

namespace Regneraeven.Core.Help;

/// <summary>One explanation block. <see cref="Example"/> lines are shown in a fixed-width font (opstillinger).</summary>
public sealed record RuleSection(string Heading, string Text, IReadOnlyList<string>? Example = null, string? Tip = null);

public sealed record RuleTopic(Topic Topic, string Intro, IReadOnlyList<RuleSection> Sections, string Remember)
{
    public TopicInfo Info => Topic.Info();
}

/// <summary>Regneregler – written for 5.–6. klasse.</summary>
public static class RuleBook
{
    public static readonly IReadOnlyList<RuleTopic> All =
    [
        new(Topic.Plus,
            "Plus betyder at lægge sammen. Resultatet kaldes en sum.",
            [
                new("Stil op", "Skriv tallene under hinanden, så enere står under enere, tiere under tiere og hundreder under hundreder.",
                    ["  1   1     ← mente", "  4 6 8", "+ 2 5 7", "-------", "  7 2 5"],
                    "Start altid fra højre – med enerne."),
                new("Mente", "Hvis en kolonne giver 10 eller mere, skriver du kun enerne og giver 1 i mente til næste kolonne. 8 + 7 = 15: skriv 5, og husk 1 i mente."),
                new("Hovedregning", "Del tallene op: 468 + 257 = 400 + 200 + 60 + 50 + 8 + 7 = 600 + 110 + 15 = 725."),
            ],
            "Rækkefølgen er ligegyldig: 3 + 8 er det samme som 8 + 3."),

        new(Topic.Minus,
            "Minus betyder at trække fra. Resultatet kaldes en differens – forskellen mellem tallene.",
            [
                new("Stil op", "Det største tal står øverst. Enere under enere.",
                    ["  7 ¹2 ¹4", "− 2  5  7", "---------", "  4  6  7"]),
                new("Lån", "Er det øverste ciffer for lille, låner du 1 fra kolonnen til venstre. 1 tier er det samme som 10 enere: 4 bliver til 14, og 2-tallet ved siden af bliver 1 mindre."),
                new("Tjek med plus", "Du kan altid tjekke et minusstykke: 724 − 257 = 467, fordi 467 + 257 = 724."),
            ],
            "Ved minus betyder rækkefølgen noget: 8 − 3 er ikke det samme som 3 − 8."),

        new(Topic.Gange,
            "Gange er hurtig plus: 4 · 6 betyder 6 + 6 + 6 + 6. Resultatet kaldes et produkt.",
            [
                new("Tabellerne", "Kan du tabellerne fra 1 til 10 udenad, går alt det andet meget hurtigere. Se under Tabeller."),
                new("Del op", "Store tal deler du op i tiere og enere og ganger hver for sig: 47 · 6 = 40 · 6 + 7 · 6 = 240 + 42 = 282."),
                new("Stil op", "Gang hvert ciffer i det øverste tal med tallet nedenunder – fra højre – og husk mente.",
                    ["   4", "   4 7", " ·   6", " -----", "  2 8 2"]),
                new("Gange med 10, 100, 1000", "Sæt et, to eller tre nuller bag på: 34 · 100 = 3400."),
            ],
            "Rækkefølgen er ligegyldig: 7 · 8 = 8 · 7. Vælg den, der er nemmest."),

        new(Topic.Division,
            "Division betyder at dele lige ud. 56 : 7 spørger: hvor mange gange går 7 op i 56? Resultatet kaldes en kvotient.",
            [
                new("Baglæns gange", "Division er gange baglæns. 56 : 7 = 8, fordi 7 · 8 = 56."),
                new("Store tal", "Del fra venstre. 432 : 6: 6 går ikke op i 4, så tag 43. 6 · 7 = 42, rest 1. Tag næste ciffer ned: 12. 6 · 2 = 12. Svaret er 72.",
                    ["432 : 6 = 72", "42", "---", " 12", " 12", " ---", "  0"]),
                new("Rest", "Går det ikke op, får du en rest: 17 : 5 = 3 rest 2 (fordi 3 · 5 = 15, og der mangler 2 op til 17)."),
            ],
            "Man kan aldrig dividere med 0."),

        new(Topic.Regnehierarki,
            "Når et regnestykke har flere regnearter, skal du regne i den rigtige rækkefølge. Ellers får du et forkert svar.",
            [
                new("Reglen", "1. Parenteser først.\n2. Så gange og division.\n3. Til sidst plus og minus.\nRegner du med samme slags, går du fra venstre mod højre."),
                new("Eksempel", "Gange før plus:", ["4 + 3 · 5", "= 4 + 15", "= 19"], "Det er ikke 7 · 5 = 35!"),
                new("Med parentes", "Parentesen bestemmer, at plus skal regnes først:", ["(4 + 3) · 5", "= 7 · 5", "= 35"]),
            ],
            "Huskeregel: Parenteser → gange/division → plus/minus."),

        new(Topic.NegativeTal,
            "Negative tal er tal under nul, fx −5. Tænk på et termometer eller en tallinje.",
            [
                new("Tallinjen", "Til højre bliver tallene større, til venstre mindre. −8 er mindre end −2.", ["−5 −4 −3 −2 −1  0  1  2  3  4  5"]),
                new("Plus og minus", "Plus et tal: gå til højre. Minus et tal: gå til venstre. 3 − 7 = −4 (start ved 3, gå 7 til venstre)."),
                new("Minus minus", "At trække et negativt tal fra er det samme som at lægge til: 5 − (−3) = 5 + 3 = 8."),
                new("Gange", "Minus gange plus giver minus: −4 · 3 = −12. Minus gange minus giver plus: −4 · (−3) = 12."),
            ],
            "Temperatur, havdybde og gæld er eksempler på negative tal i virkeligheden."),

        new(Topic.Decimaltal,
            "Decimaltal er tal med komma, fx 3,75. Cifrene efter kommaet er tiendedele, hundrededele og tusindedele.",
            [
                new("Plus og minus", "Stil op, så kommaerne står lige under hinanden. Mangler der cifre, kan du skrive et 0.",
                    ["  12,50", "+  3,75", "-------", "  16,25"]),
                new("Gange", "Gang som om der ikke var komma. Tæl derefter, hvor mange decimaler der var i alt, og sæt kommaet. 1,5 · 2,4: 15 · 24 = 360, to decimaler i alt → 3,60 = 3,6."),
                new("Gange og dividere med 10, 100, 1000", "Gange: flyt kommaet til højre (1, 2 eller 3 pladser). Division: flyt det til venstre. 4,25 · 100 = 425 og 37 : 10 = 3,7."),
            ],
            "0,5 er det samme som en halv, og 0,25 er det samme som en kvart."),

        new(Topic.Enheder,
            "En enhed fortæller, hvad du måler i: meter, gram, liter osv.",
            [
                new("Længde", "1 km = 1000 m · 1 m = 10 dm = 100 cm · 1 cm = 10 mm"),
                new("Vægt", "1 kg = 1000 g · 1 ton = 1000 kg"),
                new("Rumfang", "1 l = 10 dl = 100 cl = 1000 ml"),
                new("Omregn", "Fra stor til lille enhed ganger du (2 m = 200 cm). Fra lille til stor enhed dividerer du (750 g = 0,75 kg)."),
            ],
            "Regn altid i samme enhed, før du lægger sammen: 1 m + 30 cm = 100 cm + 30 cm = 130 cm."),

        new(Topic.Broeker,
            "En brøk er en del af noget helt. I 3/4 er 3 tælleren (hvor mange dele du har) og 4 nævneren (hvor mange dele det hele er delt i).",
            [
                new("Forlæng", "Gang tæller og nævner med det samme tal. Brøkens værdi er den samme: 2/3 = 4/6 = 8/12."),
                new("Forkort", "Del tæller og nævner med det samme tal: 6/8 = 3/4 (begge delt med 2). Forkort helt, til der ikke er flere fælles tal at dele med."),
                new("Plus og minus", "Brøkerne skal have samme nævner (fællesnævner). Så lægger du tællerne sammen.",
                    ["1/2 + 1/4", "= 2/4 + 1/4", "= 3/4"], "Nævneren lægges aldrig sammen!"),
                new("Gange", "Tæller gange tæller og nævner gange nævner: 2/3 · 3/4 = 6/12 = 1/2. Et helt tal gange en brøk: gang kun tælleren: 3 · 2/5 = 6/5 = 1 1/5."),
                new("Brøkdel af et tal", "3/5 af 40: find først 1/5 (40 : 5 = 8), og gang med 3 (8 · 3 = 24)."),
                new("Blandede tal", "7/4 er mere end en hel. 4/4 er 1, og der er 3/4 tilbage: 7/4 = 1 3/4."),
            ],
            "1/2 = 0,5 = 50 %. Brøker, decimaltal og procent kan være det samme tal skrevet på forskellige måder."),

        new(Topic.Procent,
            "Procent betyder hundrededele. 25 % er 25 ud af 100, altså 25/100 = 1/4.",
            [
                new("Procent af et tal", "25 % af 80: del med 100 og gang med 25. Eller brug genveje: 10 % = del med 10, 50 % = det halve, 25 % = del med 4."),
                new("Rabat", "En jakke koster 400 kr. med 20 % rabat. Rabatten er 20 % af 400 = 80 kr. Ny pris: 400 − 80 = 320 kr."),
                new("Hvor mange procent?", "18 ud af 60: lav en brøk 18/60 = 0,3 = 30/100 = 30 %."),
            ],
            "100 % er det hele. 10 % af et tal finder du ved at dele med 10."),

        new(Topic.Ligninger,
            "En ligning har et lighedstegn og et ukendt tal, som tit kaldes x. Du skal finde x, så begge sider er lige store.",
            [
                new("Som en vægt", "Begge sider skal veje det samme. Gør du noget på den ene side, skal du gøre det samme på den anden."),
                new("Eksempel", "Få x til at stå alene:", ["2 · x + 5 = 21", "2 · x = 16     (træk 5 fra på begge sider)", "x = 8          (del med 2 på begge sider)"]),
                new("Tjek", "Sæt dit svar ind: 2 · 8 + 5 = 21. Det passer!"),
            ],
            "Plus bliver til minus, og gange bliver til division, når du flytter et tal over på den anden side."),

        new(Topic.Geometri,
            "Geometri handler om figurer: hvor lange, hvor store og hvor skæve de er.",
            [
                new("Omkreds", "Omkreds er længden hele vejen rundt om en figur. Læg alle sider sammen. Rektangel: 2 · (længde + bredde)."),
                new("Areal", "Areal er hvor stor fladen er, målt i kvadratenheder fx cm². Rektangel: længde · bredde. Kvadrat: side · side."),
                new("Trekant", "Areal af en trekant = ½ · grundlinje · højde. Højden står vinkelret på grundlinjen."),
                new("Vinkler", "En ret vinkel er 90°. Vinklerne i en trekant er 180° tilsammen. I en firkant er de 360°."),
                new("Sammensatte figurer", "Del figuren op i rektangler, og læg arealerne sammen – eller tag et stort rektangel og træk det, der mangler, fra."),
            ],
            "Omkreds måles i cm, areal i cm². Glem ikke enheden!"),

        new(Topic.Koordinatsystem,
            "Et koordinatsystem er to tallinjer, der krydser hinanden i (0, 0). Den vandrette er x-aksen (1. aksen), den lodrette er y-aksen (2. aksen).",
            [
                new("Koordinater", "Et punkt skrives (x, y). (3, 5) betyder: gå 3 hen ad x-aksen og 5 op langs y-aksen."),
                new("Huskeregel", "Først hen ad gangen, så op ad trappen: x før y."),
                new("Negative koordinater", "Negative x-værdier ligger til venstre for 0, negative y-værdier under 0. (−2, −4) ligger nede til venstre."),
                new("Spejling", "Spejl i x-aksen: y skifter fortegn. Spejl i y-aksen: x skifter fortegn."),
            ],
            "Koordinatsystemet er delt i fire kvadranter. Første kvadrant er oppe til højre, hvor x og y begge er positive."),

        new(Topic.Tekststykke,
            "I et tekststykke står regnestykket gemt i en historie. Din opgave er at finde det.",
            [
                new("1. Læs to gange", "Hvad spørger de om? Understreg spørgsmålet."),
                new("2. Find tallene", "Skriv de tal ned, du skal bruge – og hvad de betyder (kr., stk., cm …)."),
                new("3. Vælg regneart", "I alt / tilsammen → plus. Forskel / tilbage → minus. Hver har / ad gangen → gange. Dele lige / hver får → division."),
                new("4. Svar med en sætning", "Skriv svaret med enhed: »Hun skal betale 125 kr.«"),
            ],
            "Tjek til sidst: lyder svaret fornuftigt?"),
    ];

    public static RuleTopic? For(Topic topic) => All.FirstOrDefault(r => r.Topic == topic);
}
