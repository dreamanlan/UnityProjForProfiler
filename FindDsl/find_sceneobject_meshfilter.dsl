input("Transform")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "sceneobjects");
	feature("menu", "3.Current Scene Objects/Mesh Filter");
	feature("description", "just so so");
}
filter
{
    $v0 = getcomponentinchildren(object, "MeshFilter");
    $v1 = 0;
    if(!isnull($v0)){
        if(isnull($v0.sharedMesh)){
        	$v1=1;
        };
    };
    $v1;
};