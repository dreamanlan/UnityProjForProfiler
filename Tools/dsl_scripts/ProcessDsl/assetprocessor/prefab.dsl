input("*.prefab")
{
	feature("source", "project");
	feature("menu", "0.Asset Processors/Prefab Setting");
	feature("description", "just so so");	
}
assetprocessor
{
	SetAnimatorCullCompletely;
	//ClearAnimationScaleCurve;
	SetDirty;
};