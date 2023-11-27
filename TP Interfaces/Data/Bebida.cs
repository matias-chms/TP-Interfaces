namespace TP_Interfaces.Data;

public class Bebida
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<Ingrediente> Ingredientes { get; set; }
    

    public static Bebida FernetConCoca()
    {
        return new Bebida
        {
            Name = "Fernet con Coca",
            Description = "Se destaca por su sabor amargo y distintivo, que se equilibra perfectamente con la Coca-Cola",
            Ingredientes = new List<Ingrediente>
            {
                new Ingrediente { Name = "Fernet", Proportion = 40 },
                new Ingrediente { Name = "Coca-Cola", Proportion = 60 }
            }
        };
    }

    public static Bebida Destornillador()
    {
        return new Bebida
        {
            Name = "Destornillador",
            Description = "Un cóctel sencillo compuesto por vodka y jugo de naranja",
            Ingredientes = new List<Ingrediente>
            {
                new Ingrediente { Name = "Vodka", Proportion = 50 },
                new Ingrediente { Name = "Jugo de naranja", Proportion = 50 }
            }
        };
    }

    public static Bebida GinAndTonic()
    {
        return new Bebida
        {
            Name = "Gin & Tonic",
            Description = "El Gin and Tonic es un cóctel clásico compuesto por gin y agua tónica",
            Ingredientes = new List<Ingrediente>
            {
                new Ingrediente { Name = "Ginebra", Proportion = 33 },
                new Ingrediente { Name = "Agua tónica", Proportion = 66 }
            }
        };
    }
}

