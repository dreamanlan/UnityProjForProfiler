input("*.fbx")
{
    bool("isReadable", false);
	feature("source", "project");
	feature("menu", "0.Asset Processors/Effect Fbx Setting");
	feature("description", "just so so");
}
filter
{
	if(!isReadable || isReadable && !importer.isReadable){
		1;
	}else{
		0;
	};
}
assetprocessor
{  
    SetAnimationCompressOptimal;
    SetMeshReadableTrue;
    SetMeshOptimizeGameObjectsTrue;
    SetMeshCompressOff;
    SetMeshImportMaterialsFalse;
	SetDirty;
	SaveAndReimport;
};