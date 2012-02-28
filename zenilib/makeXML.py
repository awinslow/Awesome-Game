import os
 
#path = './'
#listing = os.listdir(path)
#for infile in listing:
for root, dirs, files in os.walk("./"):
	print("*************************************") 
	print (root);
	print (dirs);
	print (files)