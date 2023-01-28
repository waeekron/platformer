using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
using Physics2DDotNet;
using Jypeli.Effects;

/// @author Walter Kronqvist
/// @version 11.12.2020
/// <summary>
///     Tasohyppely peli, jossa on tavoitteena päästä mahdollisimmaan korkealle osumatta viholliseen tai tippumatta.
/// </summary>


public class Fysiikkapeli : PhysicsGame
{   
    
    private const int LEVEYS = 500;
    private const int KORKEUS = 500;
    private const double NOPEUS = 300;
    private const double HYPPYNOPEUS = 1200;
    
    public PlatformCharacter pelaaja;
    public double alaRaja;

    private static readonly Image pelaajanKuva = LoadImage("playerCharacter.png");
    private static readonly Image hyppyAnimaatio = LoadImage("jump.png");
    private static readonly Image putoamisAnimaatio = LoadImage("fall.png");
    private static readonly Image ammusKuva = LoadImage("ammusKuva");
    private static readonly Image aseKuva = LoadImage("aseKuva");
    private static readonly Image vihuTekstuuri = LoadImage("vihu");

    private Random random = new Random();
    private int kerroin = 1;
    private int piirtoKorkeus = 780;

    private List<Vector> AlustaJoukko1 = new List<Vector>();
    private List<Vector> AlustaJoukko2 = new List<Vector>();
    private List<Vector> AlustaJoukko3 = new List<Vector>();

    private FollowerBrain seuraajanAivot = new FollowerBrain();
    private IntMeter laskuri;
    private List<Label> valikonKohdat;
    public override void Begin()
    {
        LuoKentta();
        LisaaNappaimet();
        AlustaAjastimet();  
    }


    /// <summary>
    /// Alustetaan ajastimet
    /// </summary>
    public void AlustaAjastimet()
    {
        Timer.CreateAndStart(1, LisaaTasoja);
        Timer.CreateAndStart(0.1, TarkistaPisteet);
        Timer.CreateAndStart(1, TuhoaAlustoja);
        Timer.CreateAndStart(0.01, LopetaankoPeli);
        Timer.CreateAndStart(7.5, LuoVihollisia);
    }


    /// <summary>
    /// Lisää vihollis-olion peliin.
    /// </summary>
    public void LuoVihollisia()
    {
        if (GetObjectsWithTag("vihollinen").Count >= 2) return;

        PhysicsObject vihollinen = new PhysicsObject(80, 80);
        vihollinen.LifetimeLeft = TimeSpan.FromSeconds(15.0);
        vihollinen.Tag = "vihollinen";
        vihollinen.IgnoresCollisionResponse = true;
        
        vihollinen.Y = pelaaja.Y + 800;
        vihollinen.X = random.Next(-LEVEYS,LEVEYS);
        vihollinen.Image = vihuTekstuuri;

        seuraajanAivot = new FollowerBrain(pelaaja);
        seuraajanAivot.Speed = NOPEUS/2;
        vihollinen.Brain = seuraajanAivot;

        AddCollisionHandler(vihollinen, VihollinenTormasi);
        Add(vihollinen);
    }


    /// <summary>
    /// Osuessa pelaajan asetetaan pelaaja-olio jättämään törmäykset huomioimatta
    /// </summary>
    /// <param name="vihollinen"></param>
    /// <param name="kohde"></param>
    public void VihollinenTormasi(PhysicsObject vihollinen, PhysicsObject kohde)
    {
        if (kohde.Tag.Equals("pelaaja")) 
        {
            kohde.IgnoresCollisionResponse = true;
            seuraajanAivot.Active = false;
        }
    }


    /// <summary>
    /// Jos pelaaja tippuu liian alas peli lopetetaan ja aloitetaan alusta
    /// </summary>
    public void LopetaankoPeli()
    {
        if (pelaaja.Bottom < Level.Bottom + alaRaja)
        {
            Camera.StopFollowing();
            Timer ajastin = new Timer();
            ajastin.Interval = 3.0;
            ajastin.Timeout += delegate { AloitaAlusta(); };
            ajastin.Start();
        }
    }


    /// <summary>
    /// Haetaan kaikki alustat, jotka tuhotaan jos predikaatti OnkoLiianAlhaalla == true.
    /// </summary>
    public void TuhoaAlustoja()
    {
        GetObjectsWithTag("tasoTag").RemoveAll(OnkoLiianAlhaalla);
    }


    /// <summary>
    /// Tarkistetaan onko peli alusta poistettavissa pelistä. Jos on niin olio tuhotaan.
    /// </summary>
    /// <param name="o">Pelin alusta</param>
    /// <returns>
    ///     True jos alusta on liian alhaalla
    ///     False jos alusta ei ole liian alhaalla
    ///</returns>
    public Boolean OnkoLiianAlhaalla(GameObject o)
    {      
        if (o.Y < Level.Bottom + pelaaja.Y) { o.Destroy(); return true; }
        return false;
    }

   /// <summary>
   /// Muutta pistelaskurin arvoa pelaajan korkeuden kasvaessa.
   /// </summary>
    public void TarkistaPisteet()
    {
        if (laskuri.Value > Convert.ToInt32(pelaaja.Y)) return;
        laskuri.Value = Convert.ToInt32(pelaaja.Y);
    }


    /// <summary>
    /// Tarkistetaan onko alusta pelaajan yläpuolella/ tulossa peliin.
    /// </summary>
    /// <param name="o">alusta</param>
    /// <returns></returns>
    public Boolean OnkoTasoLiianYlhaalla(GameObject o)
    {
        if (o.Y > pelaaja.Y + KORKEUS) return true;
        return false;
    }


    /// <summary>
    /// Lisätään alustajoukko peliin
    /// </summary>
    /// <param name="alustaJoukko">Peliin lisättävien alustojen koordinaatit</param>
    public void LisaaAlustaJoukko(List<Vector> alustaJoukko)
    {
        foreach (Vector piste in alustaJoukko)
        {
            PhysicsObject taso = PhysicsObject.CreateStaticObject(30, 10);              
            taso.Position = new Vector(piste.X, piste.Y + piirtoKorkeus * kerroin);
            taso.Color = Color.Wheat;
            taso.Tag = "tasoTag";
            Add(taso);
         }
        alaRaja = pelaaja.Y;
        kerroin++;
    }
    

    /// <summary>
    /// Lisaa tarvittaessa lisää tasoja peliin
    /// </summary>
    public void LisaaTasoja()
    {
        if (GetObjectsWithTag("tasoTag").FindAll(OnkoTasoLiianYlhaalla).Count > 0) // Ettei lisätä liikaa hyppy alustoja
        {
            GetObjectsWithTag("tasoTag").FindAll(OnkoLiianAlhaalla); // Poistetaan liian alas jääneet alustat.
            return;
        }
        if (pelaaja.Y < alaRaja)  return;

        int i = random.Next(0, 3);

        if (i == 0) { LisaaAlustaJoukko(AlustaJoukko1); }
        if (i == 1) { LisaaAlustaJoukko(AlustaJoukko2); }
        if (i == 2) { LisaaAlustaJoukko(AlustaJoukko3); }
    }


    /// <summary>
    /// Ladataan kenttä ja alustetaan peli.
    /// </summary>
    private void LuoKentta()
    {
        ColorTileMap kentta = ColorTileMap.FromLevelAsset("kentta1.png");
        
        kentta.SetTileMethod(Color.Black, LisaaEnsimmaisetTasot);
        kentta.SetTileMethod(Color.Red, LisaaPelaaja);
        kentta.SetTileMethod(Color.Gray, LataaAlustojenKoordinaatit, Color.Gray);
        kentta.SetTileMethod(Color.White, LataaAlustojenKoordinaatit, Color.White);

        kentta.Execute();

        LuoPistelaskuri();

        Gravity = new Vector(0, -1400);
        Level.BackgroundColor = Color.Salmon;

        Camera.FollowY(pelaaja);
        Camera.ZoomFactor = 1;
        Camera.FollowOffset = new Vector(0, 200);
    }


    /// <summary>
    /// Lisätään peliin pistelaskuri
    /// </summary>
    public void LuoPistelaskuri()
    {
        laskuri = new IntMeter(0);
        Label pisteNaytto = new Label();

        pisteNaytto.X = Screen.Left + 100;
        pisteNaytto.Y = Screen.Top - 100;
        pisteNaytto.TextColor = Color.White;
        pisteNaytto.BindTo(laskuri);
        Add(pisteNaytto);
    }
   

    /// <summary>
    /// Lisää ensimmäiset tasot peliin.
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    private void LisaaEnsimmaisetTasot(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus-10);
        taso.Position = paikka;
        taso.Tag = "tasoTag";
        taso.Color = Color.Wheat;

        AlustaJoukko3.Add(paikka);
        Add(taso);
    }


    /// <summary>
    /// Tallentaa pelikentästä alustojen koordinatit
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    private void LataaAlustojenKoordinaatit(Vector paikka, double leveys, double korkeus, Color vari)
    {
        if (vari.Equals(Color.White)) { AlustaJoukko1.Add(paikka); }
        if (vari.Equals(Color.Gray)) { AlustaJoukko2.Add(paikka); }
    }

 

    /// <summary>
    /// Lisätään pelaaja
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja = new PlatformCharacter(40 , 40);
        pelaaja.Position = paikka;
       
        pelaaja.Weapon = new PlasmaCannon(80, 80);
        pelaaja.Weapon.X = -20  ;
        pelaaja.Weapon.Angle = new Vector(0,1).Angle;
        pelaaja.Weapon.AmmoIgnoresGravity = true;
        pelaaja.Weapon.Power.DefaultValue = 2000;
        pelaaja.Weapon.ProjectileCollision = AmmusOsui;
       
        pelaaja.Weapon.Image = aseKuva;
        pelaaja.Image = pelaajanKuva;

        pelaaja.AnimJump = new Animation(hyppyAnimaatio);
        pelaaja.AnimFall = new Animation(putoamisAnimaatio);
        pelaaja.AnimIdle = new Animation(pelaajanKuva);

        alaRaja = pelaaja.Y;
        pelaaja.Tag = "pelaaja";
        Add(pelaaja);
    }


    /// <summary>
    /// Tarkistetaan osuuko ammus viholliseen
    /// </summary>
    /// <param name="ammus">Pelaajan aseen ammus</param>
    /// <param name="kohde">PhysicsObject</param>
    public void AmmusOsui(PhysicsObject ammus, PhysicsObject kohde)
    {
        if (kohde.Tag.Equals("vihollinen")) { kohde.Destroy(); }
    }


    /// <summary>
    /// Lisätään näppäin kuuntelijat
    /// </summary>
    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.A, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, -NOPEUS);
        Keyboard.Listen(Key.D, ButtonState.Down, Liikuta, "Liikkuu vasemmalle", pelaaja, NOPEUS);
        Keyboard.Listen(Key.Space, ButtonState.Pressed, Hyppaa, "Pelaaja hyppää", pelaaja, HYPPYNOPEUS);
        Keyboard.Listen(Key.W, ButtonState.Pressed, Ammu, "Ampuu", pelaaja);
        Keyboard.Listen(Key.R, ButtonState.Pressed, AloitaAlusta, "Aloittaa pelin alusta");
        PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Liikutetaan pelaajaa
    /// </summary>
    /// <param name="hahmo">pelaaja</param>
    /// <param name="nopeus">pelaajan nopeus</param>
    private void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
        if (pelaaja.Position.X > LEVEYS || pelaaja.Position.X < -LEVEYS) { pelaaja.X *= -1; } // Siirretään hahmo toiselle laidalla
    }


    /// <summary>
    /// Pelaaja hyppää
    /// </summary>
    /// <param name="hahmo">pelihahmo</param>
    /// <param name="nopeus">hypyn nopeus</param>
    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
    }


    /// <summary>
    /// Luodaan ammus jos pelaaja ampuu
    /// </summary>
    /// <param name="pelaaja">pelihahmo</param>
    public void Ammu(PlatformCharacter pelaaja)
    {
        PhysicsObject ammus = pelaaja.Weapon.Shoot();
        
        if (ammus != null)
        {
            ammus.IgnoresCollisionResponse = true;
            ammus.LifetimeLeft = TimeSpan.FromSeconds(2.0);
            ammus.Size *= 4;
            ammus.Image = ammusKuva;
        }
    }


    /// <summary>
    /// Aloittaa pelin alusta ja alustaa oliomuuttujat.
    /// </summary>
    public void AloitaAlusta()
    {
        ClearAll();
        AlustaJoukko1 = new List<Vector>();
        AlustaJoukko2 = new List<Vector>();
        AlustaJoukko3 = new List<Vector>();
        kerroin = 1;
        Begin();
    }
}




