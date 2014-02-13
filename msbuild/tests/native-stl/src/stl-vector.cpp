#include <iostream>
#include <vector>
#include <algorithm> // sort, max_element, random_shuffle, remove_if, lower_bound 
#include <functional> // greater, bind2nd
// used here for convenience, use judiciously in real programs. 
using namespace std;
 
int main()
{
  int arr[4] = {1, 2, 3, 4};
  // initialize a vector from an array
  vector<int> numbers(arr, arr+4);
  // insert more numbers into the vector
  numbers.push_back(5);
  numbers.push_back(6);
  numbers.push_back(7);
  numbers.push_back(8);
  // the vector currently holds {1, 2, 3, 4, 5, 6, 7, 8}
 
  // randomly shuffle the elements
  random_shuffle( numbers.begin(), numbers.end() );
 
  // locate the largest element, O(n)
  vector<int>::const_iterator largest = 
    max_element( numbers.begin(), numbers.end() );
 
  cout << "The largest number is " << *largest << "\n";
  cout << "It is located at index " << largest - numbers.begin() << "\n";
 
  // sort the elements
  sort( numbers.begin(), numbers.end() );
 
  // find the position of the number 5 in the vector 
  vector<int>::const_iterator five = 
    lower_bound( numbers.begin(), numbers.end(), 5 );  
 
  cout << "The number 5 is located at index " << five - numbers.begin() << "\n";
 
  // erase all the elements greater than 4   
  numbers.erase( remove_if(numbers.begin(), numbers.end(), 
    bind2nd(greater<int>(), 4) ), numbers.end() );
 
  // print all the remaining numbers
  for(vector<int>::const_iterator it = numbers.begin(); it != numbers.end(); ++it)
  {
    cout << *it << " ";
  }
 
  return 0;
}