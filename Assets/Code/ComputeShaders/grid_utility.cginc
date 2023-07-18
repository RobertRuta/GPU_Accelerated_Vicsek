// #include "simulation_variables.cginc"

void BoundToBox(inout float3 pos)
{
    if(pos.x > box.x)
    {
       pos.x -= box.x;
    }
    if(pos.x < 0.0)
    {
        pos.x += box.x;
    }
    if(pos.y > box.y)
    {
        pos.y -= box.y;
    }
    if(pos.y < 0.0)
    {
        pos.y += box.y;
    }
    if(pos.z > box.z)
    {
        pos.z -= box.z;
    }
    if(pos.z < 0.0)
    {
        pos.z += box.z;
    }
}


uint CalcCellId(int3 cell_xyz)
{
    uint X = cell_xyz.x;
    uint Y = cell_xyz.y;
    uint Z = cell_xyz.z;
    return X + grid_dims.x * Y + grid_dims.x * grid_dims.y * Z;
}

int3 CalcCellCoords(float3 pos)
{
    float cell_dim = radius;
    // cell {0, 0, 0} is found at coordinate origin and has index 0
    int cell_X = floor(pos.x / cell_dim);
    int cell_Y = floor(pos.y / cell_dim);
    int cell_Z = floor(pos.z / cell_dim);
    int3 cell_xyz = int3(cell_X, cell_Y, cell_Z);

    return cell_xyz;
}
