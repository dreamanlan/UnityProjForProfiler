input("CreateRefAsset")
{
	feature("source", "list");
	feature("menu", "6.Tools/Create Ref Asset");
	feature("description", "just so so");
}
process
{	
	createrefasset("Assets/M1Tool/ref_by_table_or_code.asset", readalllines("stringlist.txt"));
};