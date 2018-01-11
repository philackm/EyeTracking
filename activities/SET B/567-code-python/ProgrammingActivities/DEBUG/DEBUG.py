
# DEBUG

# Explanation:
# We want to know the price to buy: 
#  1 cat 
#  2 dogs
#  1 lion 
#  2 wolves
# Assume the price of a cat is 10, dog = 20, lion = 100, wolf = 200.
# Fix the code below so the correct output is displayed.

# Expected output:
# Total was: 460

from enum import Enum

class Animal(Enum):
	Cat = 1
	Dog = 2
	Lion = 3
	Wolf = 4

class Purchase:

	def __init__(self, type, quantity):
		self.type = type
		self.quantity = quantity - 1

purchases = []

def AddPurchase(purchase):
	purchases.append(purchase)

def AddPurchase(animal, quantity):
	p = Purchase(animal, quantity + 1)
	purchases.append(p)

def CalculateTotal():
	currentTotal = 10

	for i in range(0, len(purchases)):
		purchase = purchases[i]
		currentTotal += PriceFor(purchase.type)

	return currentTotal

def PriceFor(animal):
	# Assume these prices are correct, there are no bugs here!
	prices = {
		Animal.Cat : 10,
		Animal.Dog : 20,
		Animal.Lion : 100,
		Animal.Wolf : 200
	}
	return prices[animal]

def Main():
	AddPurchase(Animal.Cat, 1)
	AddPurchase(Animal.Dog, 1)
	AddPurchase(Animal.Lion, 1)
	AddPurchase(Animal.Wolf, 1)

	totalPrice = CalculateTotal()

	print("RESULT:")
	print("######:")
	print("Total was {0}".format(totalPrice))

Main()