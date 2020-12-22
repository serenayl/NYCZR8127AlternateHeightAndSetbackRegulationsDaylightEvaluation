

# NYC ZR 81-27: Daylight Evaluation

A calculator that approximates the calculations and graphics required for the daylight evaluation setback method in the NYC Zoning Resolution.

This calculator currently does not take reflectance calculations into consideration, nor does it handle the modifications available in 81-661 for the Grand Central Core Area.

This calculator only handles rectangular lots with 4 horizontal and vertical sides. If an irregular lot is given, it will use the bounding box of the site as your lot.

When following the example in the zoning resolution, most of our score numbers differ in some way from what is shown. Where possible, we made decisions that would yield a more conservative score.

It is recommended that you view the results in ortho top view, as some of the items are shifted in elevation to avoid mesh collisions.

This is an estimator and is not intended to guarantee compliance. This function has no affiliation with the NYC Department of City Planning.

This is a work in progress: if you run into any errors, please contact serena@hypar.io

This function is the result of a collaboration between Serena Li and Luis Felipe Paris. If you would like to help contribute, please reach out!

|Input Name|Type|Description|
|---|---|---|


<br>

|Output Name|Type|Description|
|---|---|---|
|Lowest Street Score|Number|A number below 66 means this design is not passing|
|Overall Daylight Score|Number|A number below 75 means the lot does not pass, or below 66 if this is in the East Midtown Subdistrict|
|Result|String|An ESTIMATE of whether your design is passing according to this calculation method.|

