# CK.Object.Processor

A processor combines a condition on an object (a `Func<object,bool>` from
[CK.Object.Predicate](../CK.Object.Predicate/README.md)) and a transform function (a `Func<object,object>`
from [CK.Object.Transform](../CK.Object.Transform/README.md). The transform function is called
when the condition evaluates to true.

A processor is ultimately a `Func<object,object?>`: a `null` result captures the fact that the
condition failed.



