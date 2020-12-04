input("*.fbx")
{
	feature("source", "project");
	feature("menu", "0.Asset Processors/Fbx Setting");
	feature("description", "just so so");
}
assetprocessor
{  
    SetAnimationCompressOptimal;
    SetMeshReadableFalse;
    SetMeshCompressOff;
    SetMeshImportMaterialsFalse;
	SetDirty;
	SaveAndReimport;
};