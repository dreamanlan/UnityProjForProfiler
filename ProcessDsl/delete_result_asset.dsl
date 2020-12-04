input
{
	feature("source", "project");
	feature("menu", "1.Project Resources/Delete Result Asset");
	feature("description", "just so so");
}
process
{
	deleteasset(assetpath);
};