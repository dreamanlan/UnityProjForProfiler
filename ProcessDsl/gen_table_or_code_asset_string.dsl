input("GenTableOrCodeAssetString")
{
	feature("source", "list");
	feature("menu", "6.Tools/Gen Table Or Code Asset String");
	feature("description", "just so so");
}
process
{	
	writealllines("stringlist.txt", buildassetstringlist("d:\\Code\\Client\\CsLibrary\\App\\rewrite\\stringlist0.txt","d:\\Code\\Client\\rewrite\\stringlist1.txt","d:\\Code\\Client\\rewrite\\stringlist2.txt","d:\\Code\\Client\\rewrite\\stringlist3.txt","d:\\Code\\Client\\rewrite\\stringlist4.txt","d:\\Code\\Client\\rewrite\\stringlist5.txt"));
};