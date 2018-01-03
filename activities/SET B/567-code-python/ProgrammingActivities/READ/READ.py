
# READ

def Question1():
	x = True
	y = 1
	z = 2
	a = 0

	x = (y - 1) == a

	if(not x):
		a = y + 1
	else:
		z = z + 1
		a = y + z

	print("a is {0}".format(a))

def Question2():
	arr = [1, 2, 3, 4]
	x = 0

	for a in arr:
		t = a * 2
		x += t
		x -= 1

	print("x is {0}".format(x))

def Question3():
	Bar()

def Foo(a):
	return (a * a) - 1

def Baz(i):
	# % is the modulus operator, e.g., x % y, returns the remainder
    # after dividing x by y.
	return (i % 2) == 0

def Bar():
	y = 2
	# range() does not include the upper bound
	for i in range(1, 5):
		if(Baz(i)):
			print("{0}".format(i + Foo(y)))
		else:
			print("0".format(Foo(i)))

def Main():
	print("Uncomment the following lines of code to see the correct results")
	# Question1()
	# Question2()
	# Question3()

Main()