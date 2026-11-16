input("*.dds")
{
	feature("source", "project");
	feature("menu", "2.Project Resources/Delete DDS Texture");
	feature("description", "just so so");
}
process
{
	delete_asset(asset_path);
};