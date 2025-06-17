# Curico

A command-line tool to convert pngs to windows Icon or Cursor files.

## Usage

```
curico ico <inputPath> [--output=<path>] [--sizes=<size;size;...]
curico cur <inputPath> [--output=<path>] [--hotspots=<size:x,y;size:x,y;...>]
```

### Arguments

- **format**: `ico` to create an icon or `cur` to create a cursor.
- **inputPath**: file/folder path. Images provided should be square.
    - File: will be resized to each of required sizes. Make sure it's at least 128x128 in size, smaller images will not look good scaled up.
    - Folder: Should provide one image of each size you want.  
- **--output**: (Optional) output file path. Default is 'output.ico'.
- **--sizes**: (Optional) Only applicable if you provide a single file to be resized and have not provided cursor hotspots list, this is the list of sizes to create. Defaults to `128;96;64;48;32`
- **--hotspots**: (Optional) Only for cursors. Hotspots per image size. Omitted ones will be `0,0`. If this option isn't passed, all will be `0,0`.
                               Format: `size1:x1,y1;size2:x2,y2;...` e.g., `128:10,10;96:6,6`

### Examples

```
curico ico my_image.png --output=output.ico
curico ico C:\path\my_image.png --output=output.ico --sizes=128;96;64;48;32
curico ico "C:\path\to\folder"
curico cur "path\to\folder" --hotspots=128:10,10;96:6,6;64:4,4
```
