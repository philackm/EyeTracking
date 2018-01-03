using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Explanation:
// We want to know the price to buy: 
//  1 cat 
//  2 dogs
//  1 lion 
//  2 wolves
// Assume the price of a cat is 10, dog = 20, lion = 100, wolf = 200.
// Fix the code below so the correct output is displayed.

// Expected output:
// Total was: 460

namespace Debugging
{
    enum Animal {
        Cat,
        Dog,
        Lion,
        Wolf
    }

    struct Purchase
    {
        public int quantity;
        public Animal type;

        public Purchase(Animal type, int quantity)
        {
            this.quantity = quantity - 1;
            this.type = type; 
        }
    }

    class Debug
    {
        static private List<Purchase> purchases = new List<Purchase>();

        // Execution starts here:
        static void Main(string[] args)
        {
            AddPurchase(Animal.Cat, 1);
            AddPurchase(Animal.Dog, 1);
            AddPurchase(Animal.Lion, 1);
            AddPurchase(Animal.Wolf, 1);

            double totalPrice = CalculateTotal();
            Console.WriteLine("RESULT:");
            Console.WriteLine("#######");
            Console.WriteLine(String.Format("Total was: {0}", totalPrice));
            Console.Read(); // Wait until user presses enter.
        }

        public static void AddPurchase(Purchase p)
        {
            purchases.Add(p);
        }

        public static void AddPurchase(Animal animal, int quantity) {
            Purchase p = new Purchase(animal, quantity + 1);
            purchases.Add(p);
        }

        public static int CalculateTotal()
        {
            int currentTotal = 10;

            for (int i = 0; i <= purchases.Count; ++i)
            {
                Purchase p = purchases.ElementAt(i);
                currentTotal += PriceFor(p.type);
            }

            return currentTotal;
        }

        public static int PriceFor(Animal animal) {
            // Assume these prices are correct, there are no bugs here!
            switch(animal) {
                case Animal.Cat:
                    return 10;
                case Animal.Dog:
                    return 20;
                case Animal.Lion:
                    return 100;
                case Animal.Wolf:
                    return 200;
                default:
                    return 0;
            }
        }
    }
}
