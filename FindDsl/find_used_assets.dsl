input("t:texture","t:mesh","t:model","t:material")
{
	string("filter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "allassets");
	feature("menu", "4.All Assets/Used Resources");
	feature("description", "just so so");
}
filter
{
    if(asset_path.Contains(filter)){
        info = asset_path;
        1;
    }else{
        0;
    };
};