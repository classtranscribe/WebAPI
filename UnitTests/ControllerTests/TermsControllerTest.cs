using ClassTranscribeDatabase.Models;
using ClassTranscribeServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.ControllerTests
{
    public class TermsControllerTest : BaseControllerTest
    {
        private readonly TermsController _controller;

        public TermsControllerTest(GlobalFixture fixture) : base(fixture)
        {
            _controller = new TermsController(_context, null);
        }

        [Fact]
        public async Task Get_Terms_Success()
        {
            var terms = new List<Term>()
            {
                new Term
                {
                    Name = "A",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(3),
                    UniversityId = "001"
                },
                new Term
                {
                    Name = "B",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(4),
                    UniversityId = "002"
                },
                new Term
                {
                    Name = "C",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(5),
                    UniversityId = "001"
                },
            };

            _context.Terms.AddRange(terms);
            _context.SaveChanges();

            var result = await _controller.GetTerms(terms[0].UniversityId);
            Assert.Equal(2, result.Value.Count());
            Assert.Equal(terms[0], result.Value.ElementAt(0));
            Assert.Equal(terms[2], result.Value.ElementAt(1));
        }

        [Fact]
        public async Task Get_Terms_Empty()
        {
            var result = await _controller.GetTerms("none");
            Assert.Empty(result.Value);

            result = await _controller.GetTerms(null);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task Get_Term_Success()
        {
            var term = new Term
            {
                Id = "001",
                Name = "A",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                UniversityId = "001"
            };

            _context.Terms.Add(term);

            var result = await _controller.GetTerm(term.Id);
            Assert.Equal(term, result.Value);
        }

        [Fact]
        public async Task Get_Term_Fail()
        {
            var result = await _controller.GetTerm("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.GetTerm(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Put_Term_Success()
        {
            var term = new Term
            {
                Id = "001",
                Name = "A",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                UniversityId = "001"
            };

            _context.Terms.Add(term);
            _context.SaveChanges();

            term.Name = "B";
            term.EndDate = DateTime.Now.AddMonths(6);

            var result = await _controller.PutTerm(term.Id, term);
            Assert.IsType<NoContentResult>(result);

            Assert.Equal(term, _context.Terms.Find(term.Id));
        }

        [Fact]
        public async Task Put_Term_Fail()
        {
            var result = await _controller.PutTerm("none", new Term());
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PutTerm("none", null);
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PutTerm(null, new Term());
            Assert.IsType<BadRequestResult>(result);

            result = await _controller.PutTerm(null, null);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Post_Term_Success()
        {
            var term = new Term
            {
                Id = "001",
                Name = "A",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                UniversityId = "001"
            };

            var result = await _controller.PostTerm(term);
            Assert.IsType<CreatedAtActionResult>(result.Result);

            Assert.Single(_context.Terms.ToList());
            Assert.Equal(term, _context.Terms.ToList()[0]);
        }

        [Fact]
        public async Task Post_Term_Fail()
        {
            var result = await _controller.PostTerm(null);
            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task Delete_Term_Success()
        {
            var term = new Term
            {
                Id = "001",
                Name = "A",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                UniversityId = "001"
            };

            _context.Terms.Add(term);
            _context.SaveChanges();

            var result = await _controller.DeleteTerm(term.Id);
            Assert.Equal(term, result.Value);
            Assert.Empty(_context.Terms.ToList());
        }

        [Fact]
        public async Task Delete_Term_Fail()
        {
            var result = await _controller.DeleteTerm("none");
            Assert.IsType<NotFoundResult>(result.Result);

            result = await _controller.DeleteTerm(null);
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}