## Instructions

The project using this analyzer needs to include a namingConfig.json file in its root. The json needs to have the following structure:

```
{
  "DisallowedTermsInClassNames": [ "Class" ], // terms that cannot appear in class names
  "DisallowedSuffixesInMembersType":
  [
    {
      "ClassSuffix": "Factory", // suffix of the classes that this rule applies to
      "DisallowedMemberSuffixes": [ "Repository" ] // suffixes of members that are not allowed to appear in this class
    }
  ] 
}
```

Additionally, the file needs to be designated as a 'C# analyzer additional file' in Visual Studio.

This can be done by right clicking the file, clicking Properties and under 'Build Action', selecting 'C# analyzer additional file'. Also, under 'Copy to Output Directory', 'Copy if newer' should be selected.
