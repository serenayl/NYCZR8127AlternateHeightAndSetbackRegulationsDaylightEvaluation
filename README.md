

# NYC ZR 81-27: Daylight Evaluation

A calculator that approximates the calculations and graphics required for the daylight evaluation setback method in the NYC Zoning Resolution.

This calculator currently does not take reflectance calculations into consideration, nor does it handle the modifications available in 81-661 for the Grand Central Core Area.

This calculator only handles rectangular lots with 4 horizontal and vertical sides. If an irregular lot is given, it will use the bounding box of the site as your lot.

When following the example in the zoning resolution, most of our score numbers differ in some way from what is shown. Where possible, we made decisions that would yield a more conservative score.

This is an estimator and is not intended to guarantee compliance. This function has no affiliation with the NYC Department of City Planning.

This is a work in progress: if you run into any errors, please contact serena@hypar.io

This function is the result of a collaboration between Serena Li and Luis Felipe Paris. If you would like to help contribute, please reach out!

|Input Name|Type|Description|
|---|---|---|
|Qualify for East Midtown Subdistrict|boolean|Whether your site is subject to the height and setback modifications specified in section 81-663. Modifications made: - Daylight blockage will be calculated at the intersection of 150' height projected downward, and using the input building from 150' upward. - There will be no encroachment penalty - Daylight credit will be given even if street continuity is on.|
|Skip Subdivide|boolean|Skip the portion of the code that subdivides your non-vertical edges into the 10' segments as specified by the code. Use this if your analysis is taking too long or timing out. Results will be less visually accurate and possibly less numerically accurate, but should give you a reasonable estimate of results. Use this with 'Debug Visualization' on in order to minimize difference between visual and numbers.|
|Vantage Streets|array|A list of vantage streets to calculate for.|
|Debug Visualization|boolean|Visualize raw plan and section angles, rather than curved projections on a modified vertical scale. This is the grid and projection that is actually used to calculate all intersections and numbers, while the final curved version is for display.|


<br>

|Output Name|Type|Description|
|---|---|---|
|Lowest Street Score|Number|A number below 66 means this design is not passing|
|Overall Daylight Score|Number|A number below 75 means the lot does not pass, or below 66 if this is in the East Midtown Subdistrict|
|Result|String|An ESTIMATE of whether your design is passing according to this calculation method.|

