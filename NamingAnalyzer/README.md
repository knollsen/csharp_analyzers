## Instructions

The project using this analyzer needs to include a namingConfig.json file in its root. The json needs to have the following structure:

{
  "DisallowedTermsInClassNames": [ "Class" ],
  "DisallowedSuffixesInMembersType": 
  [
    {
      "ClassSuffix": "Factory",
      "DisallowedMemberSuffixes": [ "Repository" ]
    }
  ] 
}